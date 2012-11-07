gxalert
=======
_Connecting rapid diagnostics with better health outcomes_



Use the GxAlertMySql.sql create-script to create the necessary tables in your MySql database.

In the database, insert the test codes you would like to handle into the resulttestcode-table. These are the codes you enter in the GeneXpert software under System Configuration -> Host Communication Settings -> Host Test Codes -> Result Test Code.

Configure your instance of GxAlert using the App.config file. The most important thing is the database connection string. You don't have to set up twilio, but if you don't SMS alerts won't work.

Configure alerts using the notifications-tables.

GxAlert uses (a slightly adapted version of) nHapi to parse the HL7 message from GeneXpert. You can find the nHapi project here: 
http://sourceforge.net/projects/nhapi/