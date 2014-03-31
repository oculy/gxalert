using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace GxAlert
{
    public class MshApi
    {
        private GxAlertEntities bpe = new GxAlertEntities();

        private string userName = ConfigurationManager.AppSettings["eTbUsername"];
        private string password = ConfigurationManager.AppSettings["eTbPassword"];
        private int workspaceId = Convert.ToInt32(ConfigurationManager.AppSettings["eTbWorkspaceId"]);

        internal void Send(int? testId)
        {
            var test = this.bpe.tests.FirstOrDefault(t => t.TestId == testId);

            if (test == null || test.deployment.Approved != true)
            {
                return;
            }
            else
            {
                this.SubmitTestToMsh(test);
            }
        }

        internal void SubmitTestToMsh(test test)
        {
            //only submit to msh if nigeria:
            if (!test.deployment.CountryId.HasValue || test.deployment.CountryId != 2)
                return;

            // very basic checks: do we have patientId, laboratoryId, is it int?
            if (string.IsNullOrWhiteSpace(test.PatientId))
            {
                Logger.Log("Patient ID is required.", LogLevel.Info);
                this.SaveAttemptToApiLog(test.TestId, "Patient ID is required.");
                return;
            }

            int patientId = 0;
            if (!int.TryParse(test.PatientId, out patientId))
            {
                Logger.Log("Patient ID must be an integer.", LogLevel.Info);
                this.SaveAttemptToApiLog(test.TestId, "Patient ID must be an integer");
                return;
            }

            if (!test.deployment.MshLaboratoryId.HasValue)
            {
                Logger.Log("The test was conducted in a deployment that has no Laboratory ID.", LogLevel.Info);
                this.SaveAttemptToApiLog(test.TestId, "The test was conducted in a deployment that has no Laboratory ID.");
                return;
            }

            // alrighty, let's mess with MSH now: 
            // Try to send the test result if the MshSessionId is not null:
            var dataExchangeResponse = new MshXpertService.response();
            var authenticationResponse = new MshAuthenticationService.response();

            if (Program.MshSessionId != null)
            {
                dataExchangeResponse = this.CallDataExchangeApi(test);
            }

            // if error=2 ->sessionID invalid -> authenticate again, then try call again
            if (dataExchangeResponse.errorno == 2 || Program.MshSessionId == null)
            {
                authenticationResponse = this.CallAuthenticationApi();
                if (authenticationResponse.errorno == 0)
                {
                    this.CallDataExchangeApi(test);
                }
            }

            switch (dataExchangeResponse.errorno)
            {
                case 0:
                    // The call was successfully executed.
                    Logger.Log("Successfully sent to eTB Manager", LogLevel.Info);
                    break;
                case 1:
                    // Authentication failed. User name, password or workspace are not valid. returned only by the authenticator service.
                    Logger.Log("Authentication Failed", LogLevel.Warning);
                    break;
                case 2:
                    // The sessionID provided is not valid. You must authenticate again.
                    Logger.Log("SessionID invalid", LogLevel.Warning);
                    break;
                case 3:
                    // Unexpected error. This is not common and it’s better to contact the system administrator when it happens. 
                    // Check the errormsg field for more information.
                    Logger.Log("Unexpected Error: " + dataExchangeResponse.errormsg, LogLevel.Error);
                    break;
                case 4:
                    // Validation error. It is caused when the information didn’t pass the validation rules of the method call, 
                    // for example, a required field that is null. Check the errormsg field for more information.
                    Logger.Log("Validation Failed: " + dataExchangeResponse.errormsg, LogLevel.Error);
                    break;
                default:
                    return;
            }
        }

        private MshXpertService.response CallDataExchangeApi(test test)
        {
            // new instance of service client
            MshXpertService.xpertServiceClient client = new MshXpertService.xpertServiceClient();

            // build result-object
            var tbResult = new MshXpertService.xpertResult();
            string tbResultText = test.testresults.First(r => r.ResultTestCodeId == 3).Result;
            #region Just a long-ish switch to match our and their result codes...
            switch (tbResultText)
            {
                case "ERROR":
                    tbResult = MshXpertService.xpertResult.ERROR;
                    break;
                case "INVALID":
                    tbResult = MshXpertService.xpertResult.INVALID;
                    break;
                case "MTB DETECTED HIGH":
                    tbResult = MshXpertService.xpertResult.TB_DETECTED;
                    break;
                case "MTB DETECTED LOW":
                    tbResult = MshXpertService.xpertResult.TB_DETECTED;
                    break;
                case "MTB DETECTED MEDIUM":
                    tbResult = MshXpertService.xpertResult.TB_DETECTED;
                    break;
                case "MTB DETECTED VERY LOW":
                    tbResult = MshXpertService.xpertResult.TB_DETECTED;
                    break;
                case "MTB NOT DETECTED":
                    tbResult = MshXpertService.xpertResult.TB_NOT_DETECTED;
                    break;
                default:
                    tbResult = MshXpertService.xpertResult.NO_RESULT;
                    break;
            }
            #endregion

            // build rif-result object
            // build result-object
            var rifResult = new MshXpertService.xpertRifResult();
            string rifResultText = test.testresults.First(r => r.ResultTestCodeId == 4).Result;
            #region Just a long-ish switch to match our and their result codes...
            switch (rifResultText)
            {
                case "ERROR":
                    rifResult = MshXpertService.xpertRifResult.RIF_INDETERMINATE;
                    break;
                case "INVALID":
                    rifResult = MshXpertService.xpertRifResult.RIF_INDETERMINATE;
                    break;
                case "Rif Resistance DETECTED":
                    rifResult = MshXpertService.xpertRifResult.RIF_DETECTED;
                    break;
                case "Rif Resistance NOT DETECTED":
                case "":
                    rifResult = MshXpertService.xpertRifResult.RIF_NOT_DETECTED;
                    break;
                default:
                    rifResult = MshXpertService.xpertRifResult.RIF_INDETERMINATE;
                    break;
            }
            #endregion

            // build test-object
            MshXpertService.xpertData data = new MshXpertService.xpertData();
            data.caseId = Convert.ToInt32(test.PatientId);
            data.caseIdSpecified = true;

            // generate our own sampleId if need be
            if (string.IsNullOrWhiteSpace(test.SampleId))
            {
                test.SampleId = this.GetNewSampleId(test.TestId);
            }

            data.sampleId = test.SampleId;

            data.sampleDateCollected = test.TestStartedOn;
            data.sampleDateCollectedSpecified = true;

            data.releaseDate = test.UpdatedOn;
            data.releaseDateSpecified = true;

            data.laboratoryId = test.deployment.MshLaboratoryId.Value;
            data.laboratoryIdSpecified = true;

            data.result = tbResult;
            data.resultSpecified = true;

            data.rifResult = rifResult;
            data.rifResultSpecified = true;

            data.comments = test.Notes;

            // send request
            MshXpertService.response response = client.postResult(Program.MshSessionId, data);

            // update test table:
            if (response.errorno == 0)
            {
                test.SendToMshSuccessOn = DateTime.Now;
            }

            // save attempt to apilog:
            this.SaveAttemptToApiLog(test.TestId, data, response);

            return response;
        }

        private MshAuthenticationService.response CallAuthenticationApi()
        {
            // new instance of service client
            MshAuthenticationService.authenticatorServiceClient client = new MshAuthenticationService.authenticatorServiceClient();

            // send request
            MshAuthenticationService.response response = client.login(this.userName, this.password, this.workspaceId);

            // save session id for future calls
            Program.MshSessionId = response.result;

            return response;
        }

        private void SaveAttemptToApiLog(int testId, MshXpertService.xpertData data, MshXpertService.response response)
        {
            apilog log = this.bpe.apilogs.Create();
            log.CaseId = data.caseId.ToString();
            log.Comments = data.comments;
            log.InitiatedBy = "BigPicture Listener";
            log.InitiatedManually = false;
            log.InitiatedOn = DateTime.Now;
            log.ErrorCode = response.errorno.ToString();
            log.ErrorMessage = response.errormsg;
            log.IsError = response.errorno != 0;
            log.LaboratoryId = data.laboratoryId.ToString();
            log.ReleaseDate = data.releaseDate;
            log.RifResult = data.rifResult.ToString();
            log.SampleDateCollected = data.sampleDateCollected;
            log.SampleId = data.sampleId;
            log.TbResult = data.result.ToString();
            log.TestId = testId;
            this.bpe.apilogs.Add(log);
            this.bpe.SaveChanges();
        }

        private void SaveAttemptToApiLog(int testId, string error)
        {
            apilog log = this.bpe.apilogs.Create();
            log.InitiatedBy = "BigPicture Listener";
            log.InitiatedManually = false;
            log.InitiatedOn = DateTime.Now;
            log.ErrorCode = "-1";
            log.ErrorMessage = error;
            log.IsError = true;
            log.TestId = testId;
            this.bpe.apilogs.Add(log);
            this.bpe.SaveChanges();
        }

        private string GetNewSampleId(int testId)
        {
            int suffixNumber = 0;
            string sampleId = string.Empty;

            // Make sure it's unique
            do
            {
                suffixNumber++;
                sampleId = "BP-" + testId + "-" + suffixNumber.ToString().PadLeft(4, '0');
            }
            while (this.bpe.tests.Any(t => t.SampleId == sampleId));

            return sampleId;
        }
    }
}
