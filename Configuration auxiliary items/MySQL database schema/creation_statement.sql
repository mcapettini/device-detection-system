-- //~-----DB setup----------------------------------------------------------
-- username:	root
-- password:	Malnati

CREATE SCHEMA `device_detection_db`;


-- //~-----tables setup------------------------------------------------------
CREATE TABLE `device_detection_db`.`configuration` (
  `configuration_id` varchar(100) NOT NULL,
  `board_id` varchar(50) NOT NULL,
  `x` double NOT NULL,
  `y` double NOT NULL,
  `order` int(10) unsigned NOT NULL,
  `note` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`configuration_id`,`board_id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;


CREATE TABLE `device_detection_db`.`position` (
  `timestamp` datetime(3) NOT NULL,
  `MACaddress` varchar(20) NOT NULL,
  `configuration_id` varchar(45) NOT NULL,
  `x` double NOT NULL,
  `y` double NOT NULL,
  `sequence_number` int(11) DEFAULT NULL,
  `SSID` varchar(100) DEFAULT NULL,
  `fingerprint` int(10) unsigned DEFAULT NULL,
  PRIMARY KEY (`timestamp`,`MACaddress`,`configuration_id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;


CREATE TABLE `device_detection_db`.`log` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `timestamp` datetime(3) NOT NULL,
  `configuration` varchar(100) DEFAULT NULL,
  `type` varchar(45) NOT NULL,
  `number_boards` int(11) NOT NULL,
  `message` varchar(256) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=latin1;
