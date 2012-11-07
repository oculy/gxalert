CREATE DATABASE  IF NOT EXISTS `gxalert` /*!40100 DEFAULT CHARACTER SET utf8 */;
USE `gxalert`;
-- MySQL dump 10.13  Distrib 5.5.16, for Win32 (x86)
--
-- Host: 96.127.33.204    Database: gxalert
-- ------------------------------------------------------
-- Server version	5.1.66-community

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `notificationresult`
--

DROP TABLE IF EXISTS `notificationresult`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `notificationresult` (
  `NotificationResultId` int(11) NOT NULL AUTO_INCREMENT,
  `NotificationId` int(11) NOT NULL,
  `ResultTestCodeId` int(11) NOT NULL,
  `Result` varchar(45) NOT NULL,
  PRIMARY KEY (`NotificationResultId`),
  KEY `NotificationResultNotification` (`NotificationId`),
  KEY `ResultTestCodeId` (`ResultTestCodeId`),
  CONSTRAINT `NotificationResultNotification` FOREIGN KEY (`NotificationId`) REFERENCES `notification` (`NotificationId`) ON DELETE CASCADE ON UPDATE NO ACTION,
  CONSTRAINT `ResultTestCodeId` FOREIGN KEY (`ResultTestCodeId`) REFERENCES `resulttestcode` (`ResultTestCodeId`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `lga`
--

DROP TABLE IF EXISTS `lga`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `lga` (
  `LgaId` int(11) NOT NULL AUTO_INCREMENT,
  `StateId` int(11) DEFAULT NULL,
  `Name` varchar(255) NOT NULL,
  PRIMARY KEY (`LgaId`),
  KEY `StateLga` (`StateId`),
  CONSTRAINT `StateLga` FOREIGN KEY (`StateId`) REFERENCES `state` (`StateId`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=778 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `rawmessage`
--

DROP TABLE IF EXISTS `rawmessage`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `rawmessage` (
  `RawMessageId` int(11) NOT NULL AUTO_INCREMENT,
  `TestId` int(11) DEFAULT NULL,
  `Message` text NOT NULL,
  PRIMARY KEY (`RawMessageId`),
  KEY `RawMessageTest` (`TestId`),
  CONSTRAINT `RawMessageTest` FOREIGN KEY (`TestId`) REFERENCES `test` (`TestId`) ON DELETE SET NULL ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=1248 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `notificationstate`
--

DROP TABLE IF EXISTS `notificationstate`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `notificationstate` (
  `NotificationStateId` int(11) NOT NULL AUTO_INCREMENT,
  `NotificationId` int(11) NOT NULL,
  `StateId` int(11) NOT NULL,
  PRIMARY KEY (`NotificationStateId`),
  KEY `NotificationStateNotification` (`NotificationId`),
  KEY `NotificationStateState` (`StateId`),
  CONSTRAINT `NotificationStateNotification` FOREIGN KEY (`NotificationId`) REFERENCES `notification` (`NotificationId`) ON DELETE CASCADE ON UPDATE NO ACTION,
  CONSTRAINT `NotificationStateState` FOREIGN KEY (`StateId`) REFERENCES `state` (`StateId`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `resulttestcode`
--

DROP TABLE IF EXISTS `resulttestcode`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `resulttestcode` (
  `ResultTestCodeId` int(11) NOT NULL AUTO_INCREMENT,
  `ResultTestCode` varchar(255) NOT NULL,
  `Description` varchar(1200) DEFAULT NULL,
  `InsertedOn` datetime NOT NULL,
  `InsertedBy` varchar(255) NOT NULL,
  `UpdatedOn` datetime NOT NULL,
  `UpdatedBy` varchar(255) NOT NULL,
  PRIMARY KEY (`ResultTestCodeId`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `deployment`
--

DROP TABLE IF EXISTS `deployment`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `deployment` (
  `DeploymentId` int(11) NOT NULL AUTO_INCREMENT,
  `HostId` varchar(45) NOT NULL,
  `Description` varchar(1200) DEFAULT NULL,
  `CountryId` int(11) DEFAULT NULL,
  `RegionId` int(11) DEFAULT NULL,
  `StateId` int(11) DEFAULT NULL,
  `LgaId` int(11) DEFAULT NULL,
  `City` varchar(255) DEFAULT NULL,
  `Street1` varchar(255) DEFAULT NULL,
  `Street2` varchar(255) DEFAULT NULL,
  `Zip` varchar(45) DEFAULT NULL,
  `Latitude` float(10,6) DEFAULT NULL,
  `Longitude` float(10,6) DEFAULT NULL,
  `InsertedOn` datetime NOT NULL,
  `Insertedby` varchar(255) NOT NULL,
  `UpdatedOn` datetime NOT NULL,
  `Updatedby` varchar(255) NOT NULL,
  `Approved` bit(1) NOT NULL,
  `ApprovedOn` datetime DEFAULT NULL,
  `ApprovedBy` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`DeploymentId`),
  KEY `Country` (`CountryId`),
  KEY `Region` (`RegionId`),
  KEY `State` (`StateId`),
  KEY `Lga` (`LgaId`),
  KEY `DeploymentCountry` (`CountryId`),
  KEY `DeploymentRegion` (`RegionId`),
  KEY `DeploymentState` (`StateId`),
  KEY `DeploymentLga` (`LgaId`),
  CONSTRAINT `DeploymentCountry` FOREIGN KEY (`CountryId`) REFERENCES `country` (`CountryId`) ON DELETE SET NULL ON UPDATE NO ACTION,
  CONSTRAINT `DeploymentLga` FOREIGN KEY (`LgaId`) REFERENCES `lga` (`LgaId`) ON DELETE SET NULL ON UPDATE NO ACTION,
  CONSTRAINT `DeploymentRegion` FOREIGN KEY (`RegionId`) REFERENCES `region` (`RegionId`) ON DELETE SET NULL ON UPDATE NO ACTION,
  CONSTRAINT `DeploymentState` FOREIGN KEY (`StateId`) REFERENCES `state` (`StateId`) ON DELETE SET NULL ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=49 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `device`
--

DROP TABLE IF EXISTS `device`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `device` (
  `DeviceId` int(11) NOT NULL AUTO_INCREMENT,
  `Serial` varchar(45) NOT NULL,
  `CurrentDeploymentId` int(11) NOT NULL,
  `InsertedOn` datetime NOT NULL,
  `InsertedBy` varchar(255) NOT NULL,
  `UpdatedOn` datetime NOT NULL,
  `UpdatedBy` varchar(255) NOT NULL,
  PRIMARY KEY (`DeviceId`),
  KEY `CurrentLocation` (`CurrentDeploymentId`),
  KEY `DeviceDeployment` (`CurrentDeploymentId`),
  CONSTRAINT `DeviceDeployment` FOREIGN KEY (`CurrentDeploymentId`) REFERENCES `deployment` (`DeploymentId`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=93 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `notificationlog`
--

DROP TABLE IF EXISTS `notificationlog`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `notificationlog` (
  `NotificationLogId` int(11) NOT NULL AUTO_INCREMENT,
  `NotificationId` int(11) DEFAULT NULL,
  `NotificationName` varchar(255) NOT NULL,
  `PersonId` int(11) DEFAULT NULL,
  `PersonName` varchar(500) NOT NULL,
  `Subject` varchar(140) DEFAULT NULL,
  `Body` varchar(1200) DEFAULT NULL,
  `Sms` bit(1) NOT NULL,
  `Email` bit(1) NOT NULL,
  `Phone` bit(1) NOT NULL,
  `SentOn` datetime NOT NULL,
  `SentBy` varchar(255) NOT NULL,
  PRIMARY KEY (`NotificationLogId`),
  KEY `NotificationLogNotification` (`NotificationId`),
  KEY `NotificationLogPerson` (`PersonId`),
  CONSTRAINT `NotificationLogNotification` FOREIGN KEY (`NotificationId`) REFERENCES `notification` (`NotificationId`) ON DELETE SET NULL ON UPDATE NO ACTION,
  CONSTRAINT `NotificationLogPerson` FOREIGN KEY (`PersonId`) REFERENCES `person` (`PersonId`) ON DELETE SET NULL ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=47 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `notificationdeployment`
--

DROP TABLE IF EXISTS `notificationdeployment`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `notificationdeployment` (
  `NotificationDeploymentId` int(11) NOT NULL AUTO_INCREMENT,
  `NotificationId` int(11) NOT NULL,
  `DeploymentId` int(11) NOT NULL,
  PRIMARY KEY (`NotificationDeploymentId`),
  KEY `NotificationDeploymentNotification` (`NotificationId`),
  KEY `NotificationDeploymentDeployment` (`DeploymentId`),
  CONSTRAINT `NotificationDeploymentNotification` FOREIGN KEY (`NotificationId`) REFERENCES `notification` (`NotificationId`) ON DELETE CASCADE ON UPDATE NO ACTION,
  CONSTRAINT `NotificationDeploymentDeployment` FOREIGN KEY (`DeploymentId`) REFERENCES `deployment` (`DeploymentId`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `notificationlga`
--

DROP TABLE IF EXISTS `notificationlga`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `notificationlga` (
  `NotificationLgaId` int(11) NOT NULL AUTO_INCREMENT,
  `NotificationId` int(11) NOT NULL,
  `LgaId` int(11) NOT NULL,
  PRIMARY KEY (`NotificationLgaId`),
  KEY `NotificationLgaNotification` (`NotificationId`),
  KEY `NotificationLgaLga` (`LgaId`),
  CONSTRAINT `NotificationLgaNotification` FOREIGN KEY (`NotificationId`) REFERENCES `notification` (`NotificationId`) ON DELETE CASCADE ON UPDATE NO ACTION,
  CONSTRAINT `NotificationLgaLga` FOREIGN KEY (`LgaId`) REFERENCES `lga` (`LgaId`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `testresult`
--

DROP TABLE IF EXISTS `testresult`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `testresult` (
  `TestResultId` int(11) NOT NULL AUTO_INCREMENT,
  `TestId` int(11) NOT NULL,
  `ResultTestCodeId` int(11) NOT NULL,
  `Result` varchar(255) NOT NULL,
  `InsertedOn` datetime NOT NULL,
  `InsertedBy` varchar(255) NOT NULL,
  `UpdatedOn` datetime NOT NULL,
  `UpdatedBy` varchar(255) NOT NULL,
  PRIMARY KEY (`TestResultId`),
  KEY `ResultTestCodeTestResult` (`ResultTestCodeId`),
  KEY `TestTestResult` (`TestId`),
  CONSTRAINT `ResultTestCodeTestResult` FOREIGN KEY (`ResultTestCodeId`) REFERENCES `resulttestcode` (`ResultTestCodeId`) ON DELETE CASCADE ON UPDATE NO ACTION,
  CONSTRAINT `TestTestResult` FOREIGN KEY (`TestId`) REFERENCES `test` (`TestId`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=3123 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `devicedeploymenthistory`
--

DROP TABLE IF EXISTS `devicedeploymenthistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `devicedeploymenthistory` (
  `DeviceDeploymentHistoryId` int(11) NOT NULL AUTO_INCREMENT,
  `DeviceId` int(11) NOT NULL,
  `DeploymentId` int(11) NOT NULL,
  `InsertedOn` datetime NOT NULL,
  `InsertedBy` varchar(45) NOT NULL,
  PRIMARY KEY (`DeviceDeploymentHistoryId`),
  KEY `Device` (`DeviceId`),
  KEY `Location` (`DeploymentId`),
  CONSTRAINT `HistoryDeployment` FOREIGN KEY (`DeploymentId`) REFERENCES `deployment` (`DeploymentId`) ON DELETE CASCADE ON UPDATE NO ACTION,
  CONSTRAINT `HistoryDevice` FOREIGN KEY (`DeviceId`) REFERENCES `device` (`DeviceId`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=47 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `country`
--

DROP TABLE IF EXISTS `country`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `country` (
  `CountryId` int(11) NOT NULL AUTO_INCREMENT,
  `Name` varchar(255) NOT NULL,
  `Abbreviation` varchar(10) NOT NULL,
  `Culture` varchar(5) NOT NULL,
  PRIMARY KEY (`CountryId`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `region`
--

DROP TABLE IF EXISTS `region`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `region` (
  `RegionId` int(11) NOT NULL AUTO_INCREMENT,
  `CountryId` int(11) NOT NULL,
  `Name` varchar(255) NOT NULL,
  PRIMARY KEY (`RegionId`),
  KEY `RegionCountry` (`CountryId`),
  CONSTRAINT `RegionCountry` FOREIGN KEY (`CountryId`) REFERENCES `country` (`CountryId`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `person`
--

DROP TABLE IF EXISTS `person`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `person` (
  `PersonId` int(11) NOT NULL AUTO_INCREMENT,
  `FirstName` varchar(255) NOT NULL,
  `LastName` varchar(255) NOT NULL,
  `Email` varchar(500) DEFAULT NULL,
  `Phone` varchar(45) DEFAULT NULL,
  `Cell` varchar(45) DEFAULT NULL,
  `CountryId` int(11) DEFAULT NULL,
  `RegionId` int(11) DEFAULT NULL,
  `StateId` int(11) DEFAULT NULL,
  `LgaId` int(11) DEFAULT NULL,
  PRIMARY KEY (`PersonId`),
  KEY `PersonCountry` (`CountryId`),
  KEY `PersonRegion` (`RegionId`),
  KEY `PersonState` (`StateId`),
  KEY `PersonLga` (`LgaId`),
  CONSTRAINT `PersonCountry` FOREIGN KEY (`CountryId`) REFERENCES `country` (`CountryId`) ON DELETE SET NULL ON UPDATE NO ACTION,
  CONSTRAINT `PersonLga` FOREIGN KEY (`LgaId`) REFERENCES `lga` (`LgaId`) ON DELETE SET NULL ON UPDATE NO ACTION,
  CONSTRAINT `PersonRegion` FOREIGN KEY (`RegionId`) REFERENCES `region` (`RegionId`) ON DELETE SET NULL ON UPDATE NO ACTION,
  CONSTRAINT `PersonState` FOREIGN KEY (`StateId`) REFERENCES `state` (`StateId`) ON DELETE SET NULL ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `state`
--

DROP TABLE IF EXISTS `state`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `state` (
  `StateId` int(11) NOT NULL AUTO_INCREMENT,
  `CountryId` int(11) NOT NULL,
  `Name` varchar(255) NOT NULL,
  `Abbreviation` varchar(10) DEFAULT NULL,
  PRIMARY KEY (`StateId`),
  KEY `StateCountry` (`CountryId`),
  CONSTRAINT `StateCountry` FOREIGN KEY (`CountryId`) REFERENCES `country` (`CountryId`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=40 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `notificationperson`
--

DROP TABLE IF EXISTS `notificationperson`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `notificationperson` (
  `NotificationPersonId` int(11) NOT NULL AUTO_INCREMENT,
  `NotificationId` int(11) NOT NULL,
  `PersonId` int(11) NOT NULL,
  `Sms` bit(1) NOT NULL,
  `Email` bit(1) NOT NULL,
  `Phone` bit(1) NOT NULL,
  PRIMARY KEY (`NotificationPersonId`),
  KEY `NotificationPersonNotification` (`NotificationId`),
  KEY `NotificationPersonPerson` (`PersonId`),
  CONSTRAINT `NotificationPersonNotification` FOREIGN KEY (`NotificationId`) REFERENCES `notification` (`NotificationId`) ON DELETE CASCADE ON UPDATE NO ACTION,
  CONSTRAINT `NotificationPersonPerson` FOREIGN KEY (`PersonId`) REFERENCES `person` (`PersonId`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `notificationcountry`
--

DROP TABLE IF EXISTS `notificationcountry`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `notificationcountry` (
  `NotificationCountryId` int(11) NOT NULL AUTO_INCREMENT,
  `NotificationId` int(11) NOT NULL,
  `CountryId` int(11) NOT NULL,
  PRIMARY KEY (`NotificationCountryId`),
  KEY `NotificationCountryNotification` (`NotificationId`),
  KEY `NotificationCountryCountry` (`CountryId`),
  CONSTRAINT `NotificationCountryNotification` FOREIGN KEY (`NotificationId`) REFERENCES `notification` (`NotificationId`) ON DELETE CASCADE ON UPDATE NO ACTION,
  CONSTRAINT `NotificationCountryCountry` FOREIGN KEY (`CountryId`) REFERENCES `country` (`CountryId`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `notification`
--

DROP TABLE IF EXISTS `notification`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `notification` (
  `NotificationId` int(11) NOT NULL AUTO_INCREMENT,
  `NotificationName` varchar(255) NOT NULL,
  `EmailSubject` varchar(255) DEFAULT NULL,
  `EmailBody` varchar(1200) DEFAULT NULL,
  `SmsBody` varchar(1200) DEFAULT NULL,
  `PhoneBody` varchar(1200) DEFAULT NULL,
  `InsertedOn` datetime NOT NULL,
  `InsertedBy` varchar(255) NOT NULL,
  `UpdatedOn` datetime NOT NULL,
  `UpdatedBy` varchar(255) NOT NULL,
  PRIMARY KEY (`NotificationId`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `test`
--

DROP TABLE IF EXISTS `test`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `test` (
  `TestId` int(11) NOT NULL AUTO_INCREMENT,
  `DeploymentId` int(11) NOT NULL,
  `MessageSentOn` datetime NOT NULL,
  `SenderVersion` varchar(10) NOT NULL,
  `SenderUser` varchar(255) NOT NULL,
  `SenderIp` varchar(45) NOT NULL,
  `PatientId` varchar(45) DEFAULT NULL,
  `TestStartedOn` datetime NOT NULL,
  `TestEndedOn` datetime NOT NULL,
  `AssayHostTestCode` varchar(255) NOT NULL,
  `CartridgeSerial` varchar(45) NOT NULL,
  `CartridgeExpirationDate` datetime NOT NULL,
  `ReagentLotId` varchar(45) NOT NULL,
  `SystemName` varchar(255) NOT NULL,
  `ModuleSerial` varchar(45) NOT NULL,
  `ComputerName` varchar(255) NOT NULL,
  `AssayName` varchar(255) NOT NULL,
  `AssayVersion` varchar(10) NOT NULL,
  `ResultText` varchar(1200) NOT NULL,
  `SampleId` varchar(45) NOT NULL,
  `Notes` text,
  `InsertedOn` datetime NOT NULL,
  `InsertedBy` varchar(255) NOT NULL,
  `UpdatedOn` datetime NOT NULL,
  `UpdatedBy` varchar(255) NOT NULL,
  PRIMARY KEY (`TestId`),
  KEY `LocationTest` (`DeploymentId`),
  KEY `TestDeployment` (`DeploymentId`),
  CONSTRAINT `TestDeployment` FOREIGN KEY (`DeploymentId`) REFERENCES `deployment` (`DeploymentId`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=1014 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `notificationregion`
--

DROP TABLE IF EXISTS `notificationregion`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `notificationregion` (
  `NotificationRegionId` int(11) NOT NULL AUTO_INCREMENT,
  `NotificationId` int(11) NOT NULL,
  `RegionId` int(11) NOT NULL,
  PRIMARY KEY (`NotificationRegionId`),
  KEY `NotificationRegionNotification` (`NotificationId`),
  KEY `NotificationRegionRegion` (`RegionId`),
  CONSTRAINT `NotificationRegionNotification` FOREIGN KEY (`NotificationId`) REFERENCES `notification` (`NotificationId`) ON DELETE CASCADE ON UPDATE NO ACTION,
  CONSTRAINT `NotificationRegionRegion` FOREIGN KEY (`RegionId`) REFERENCES `region` (`RegionId`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2012-11-07 10:46:16
