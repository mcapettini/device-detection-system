// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include "Position.h"
#include "Synchronizer.h"

// for get_time() function
#include <iomanip>



//#include "mysql_driver.h"	//requires 'Progetto->Proprietà->C/C++->Generale/Directory di inclusione aggiuntive' and adding the path of the MySQL ConnectorC++

/*
 * Include directly the different
 * headers from cppconn/ and mysql_driver.h + mysql_util.h
 * (and mysql_connection.h). This will reduce the build time
 */

#include "mysql_connection.h"

#include <cppconn/driver.h>
#include <cppconn/exception.h>
#include <cppconn/resultset.h>
#include <cppconn/statement.h>
#include <cppconn/prepared_statement.h>



// ~-----namespaces-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
using namespace std;



// ~-----fields initialization------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
// database credential
string	Position::host		= "tcp://127.0.0.1:3306";
string	Position::user		= "root";
string	Position::password	= "Malnati";




// ~-----constructors and destructors-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Position::Position(
	std::chrono::milliseconds timestamp,
	std::string MACaddress_device,
	std::string configuration_id,
	Coordinates coord)
{
	this->_timestamp = timestamp;
	this->_device_id = MACaddress_device;
	this->_configuration_id = configuration_id;
	this->_coordinates = coord;
	this->_is_local = false;
	this->_sequence_number = 0;
	this->_network_id = "";
	this->_hash_TLVframebody = 0;
}


Position::Position(
	std::chrono::milliseconds timestamp,
	std::string MACaddress_device,
	std::string configuration_id,
	Coordinates coord,
	unsigned int sequence_number,
	std::string SSID,
	unsigned int fingerprint)
{
	this->_timestamp = timestamp;
	this->_device_id = MACaddress_device;
	this->_configuration_id = configuration_id;
	this->_coordinates = coord;
	this->_is_local = true;
	this->_sequence_number = sequence_number;
	this->_network_id = SSID;
	this->_hash_TLVframebody = fingerprint;
}

/* Nicolò:
 * move constructur
 */
Position::Position(Position&& that) {
	swap(*this, that);
}

/* Nicolò:
 * copy constructor
 */
Position::Position(const Position& source) {
	this->_timestamp = chrono::milliseconds(source._timestamp);
	this->_device_id = source._device_id;
	this->_configuration_id = source._configuration_id;
	this->_coordinates = source._coordinates;
	this->_is_local = source._is_local;
	this->_sequence_number = source._sequence_number;
	this->_network_id = source._network_id;
	this->_hash_TLVframebody = source._hash_TLVframebody;
}



// ~-----methods--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * method that takes care of inserting the information of the current position, in the DB
 */
void Position::insertDB() {
	// ~-----local variables--------------------------------------------
	sql::Driver				*driver				= nullptr;
	sql::Connection			*connection			= nullptr;
	sql::PreparedStatement	*prepared_statement = nullptr;

	try {
		// ~-----create a connection----------------------------------------
		driver = get_driver_instance();
		connection = driver->connect(Position::host.c_str(), Position::user.c_str(), Position::password.c_str());

		// ~-----connect to the MySQL database------------------------------
		connection->setSchema("device_detection_db");

		// ~-----create SQL query-------------------------------------------
		string query = "INSERT INTO position (timestamp, MACaddress, configuration_id, x, y, sequence_number, SSID, fingerprint) VALUES (?, ?, ?, ?, ?, ?, ?, ?)";
		prepared_statement = connection->prepareStatement(query);

		prepared_statement->setDateTime(1, parse_to_datetime(_timestamp));
		prepared_statement->setString(2, _device_id);
		prepared_statement->setString(3, _configuration_id);
		prepared_statement->setDouble(4, _coordinates.x());
		prepared_statement->setDouble(5, _coordinates.y());

		if (_is_local) {
			prepared_statement->setUInt(6, _sequence_number);
			prepared_statement->setString(7, _network_id);
			prepared_statement->setUInt(8, _hash_TLVframebody);
		}
		else {
			prepared_statement->setNull(6, 0);
			prepared_statement->setNull(7, 0);
			prepared_statement->setNull(8, 0);
		}

		// ~-----execute SQL query------------------------------------------
		prepared_statement->execute();

#if DEBUG
		cout << "DB insertion completed successfully!" << flush << endl;
#endif

		// ~-----free resources---------------------------------------------
		delete prepared_statement;
		delete connection;
	}
	catch (sql::SQLException &e) {

		// ~-----free resources---------------------------------------------
		delete prepared_statement;
		delete connection;


		// ~-----console output---------------------------------------------
#if DEBUG
		std::cout << "# ERR: SQLException in " << __FILE__;
		std::cout << "(" << __FUNCTION__ << ") on line " << __LINE__ << flush << endl;
		std::cout << "# ERR: " << e.what();
		std::cout << " (MySQL error code: " << e.getErrorCode();
		std::cout << ", SQLState: " << e.getSQLState() << " )" << flush << endl;
#endif

		// ~-----duplicated keys: simply discard insertion------------------
		if (e.getErrorCode() == 1062)	//ER_DUP_KEY
			throw Interpolation_Exception("Duplicated key in the DB: same time+MAC+confID");
		if (e.getSQLState() == "23000")
			throw Interpolation_Exception("Duplicated key in the DB: same time+MAC+confID");


		// ~-----notify the error to the C#---------------------------------
		Synchronizer::report_error(Synchronizer::UnreachableDatabase,
			"Impossible to store the position of " + _device_id + " (" + to_string(_coordinates.x()) + " : " + to_string(_coordinates.y()) + ") in the DB");
	}
}


/* Nicolò:
 * selects local (hidden) addresses in the last minutes
 */
vector<Position> Position::get_past_local_addresses() {
	// ~-----local variables------------------------------------------------
	// MySQL connection
	sql::Driver				*driver				= nullptr;
	sql::Connection			*connection			= nullptr;
	sql::PreparedStatement	*prepared_statement = nullptr;
	sql::ResultSet			*result_set			= nullptr;

	// SQL parameters
	int	minute_range = 5;

	// collection
	vector<Position> list;


	try {
		// ~-----create a connection----------------------------------------
		driver = get_driver_instance();
		connection = driver->connect(Position::host.c_str(), Position::user.c_str(), Position::password.c_str());

		// ~-----connect to the MySQL database------------------------------
		connection->setSchema("device_detection_db");

		// ~-----create SQL query-------------------------------------------
		string query = "SELECT * FROM position WHERE sequence_number IS NOT NULL AND SSID IS NOT NULL AND fingerprint IS NOT NULL AND TIMESTAMPDIFF(MINUTE, timestamp, ?) < ? AND TIMESTAMPDIFF(MINUTE, timestamp, ?) >= 0 ORDER BY timestamp ASC";
		prepared_statement = connection->prepareStatement(query);

		prepared_statement->setString(1, Position::datetime_now());
		prepared_statement->setInt(2, minute_range);
		prepared_statement->setString(3, Position::datetime_now());


		// ~-----execute SQL query------------------------------------------
		result_set = prepared_statement->executeQuery();

		// ~-----retrieve selected tuples-----------------------------------
		while (result_set->next()) {
			// convert tuple to local variables
			string datetime = result_set->getString("timestamp");
			string MACaddress = result_set->getString("MACaddress");
			string configuration = result_set->getString("configuration_id");
			double x = result_set->getDouble("x");
			double y = result_set->getDouble("y");
			Coordinates c(x, y);
			unsigned int sequence_number = result_set->getUInt("sequence_number");
			string SSID = result_set->getString("SSID");
			unsigned int fingerprint = result_set->getUInt("fingerprint");


			// convert datetime to timestamp
			chrono::milliseconds m = parse_to_timestamp(datetime);

			// insert Position object
			Position p(m, MACaddress, configuration, c);
			if (!result_set->isNull("sequence_number") || !result_set->isNull("SSID") || !result_set->isNull("fingerprint"))
				p.set_private_MACaddress(sequence_number, SSID, fingerprint);
			list.push_back(p);
		}

		// ~-----free resources---------------------------------------------
		delete result_set;
		delete prepared_statement;
		delete connection;

	}
	catch (sql::SQLException &e) {

		// ~-----free resources---------------------------------------------
		delete result_set;
		delete prepared_statement;
		delete connection;


		// ~-----console output---------------------------------------------
#if DEBUG
		std::cout << "# ERR: SQLException in " << __FILE__;
		std::cout << "(" << __FUNCTION__ << ") on line " << __LINE__ << flush << endl;
		std::cout << "# ERR: " << e.what();
		std::cout << " (MySQL error code: " << e.getErrorCode();
		std::cout << ", SQLState: " << e.getSQLState() << " )" << flush << endl;
#endif

		// ~-----notify the error to the C#---------------------------------
		Synchronizer::report_error(Synchronizer::UnreachableDatabase, "Impossible to retrieve past local (private) addresses from the DB");
	}

	return list;
}


/* Nicolò:
 * updates local (hidden) addresses once discovered they belongs to the same device
 */
void Position::update_past_local_addresses(
	std::string MACaddress_new,
	std::vector<std::string> MACaddresses_old,
	std::vector<std::chrono::milliseconds> timestamps)
{
	// ~-----local variables------------------------------------------------
	// MySQL connection
	sql::Driver				*driver				= nullptr;
	sql::Connection			*connection			= nullptr;
	sql::PreparedStatement	*prepared_statement	= nullptr;

	// SQL parameters
	int affected_rows = 0;


	// ~-----check inpput parameters----------------------------------------
	if (MACaddresses_old.size() != timestamps.size())
		throw exception("Impossible to univocally identify some tuples: vector's sizes differ");

	try {
		// ~-----create a connection----------------------------------------
		driver = get_driver_instance();
		connection = driver->connect(Position::host.c_str(), Position::user.c_str(), Position::password.c_str());


		// ~-----connect to the MySQL database------------------------------
		connection->setSchema("device_detection_db");


		// ~-----create SQL query-------------------------------------------
		string query = "UPDATE position SET MACaddress = ? WHERE MACaddress = ? AND timestamp = ?";
		prepared_statement = connection->prepareStatement(query);


		vector<string>::iterator iter_mac = MACaddresses_old.begin();
		vector<chrono::milliseconds>::iterator iter_time = timestamps.begin();
		for (; iter_mac != MACaddresses_old.end() && iter_time != timestamps.end(); iter_mac++, iter_time++) {
			// ~-----set parameters-----------------------------------------
			prepared_statement->clearParameters();

			string curr_mac = *iter_mac;
			chrono::milliseconds curr_time = *iter_time;

			prepared_statement->setString(1, MACaddress_new);
			prepared_statement->setString(2, curr_mac);
			prepared_statement->setDateTime(3, parse_to_datetime(curr_time));


			// ~-----execute SQL query------------------------------------------
			affected_rows += prepared_statement->executeUpdate();
		}

#if DEBUG
		cout << "Updated " << affected_rows << " rows in 'position' table" << flush << endl;
#endif

		// ~-----free resources---------------------------------------------
		delete prepared_statement;
		delete connection;

	}
	catch (sql::SQLException &e) {

		// ~-----free resources---------------------------------------------
		delete prepared_statement;
		delete connection;


		// ~-----console output---------------------------------------------
#if DEBUG
		std::cout << "# ERR: SQLException in " << __FILE__;
		std::cout << "(" << __FUNCTION__ << ") on line " << __LINE__ << flush << endl;
		std::cout << "# ERR: " << e.what();
		std::cout << " (MySQL error code: " << e.getErrorCode();
		std::cout << ", SQLState: " << e.getSQLState() << " )" << flush << endl;
#endif
		// ~-----notify the error to the C#---------------------------------
		Synchronizer::report_error(Synchronizer::UnreachableDatabase, "Impossible to update past local (private) addresses in the DB");
	}
}


/* Nicolò:
 * returns the human readable version of timestamp
 */
std::string Position::parse_to_datetime(std::chrono::milliseconds timestamp) {
	std::chrono::system_clock::time_point tp(timestamp);
	time_t		t = std::chrono::system_clock::to_time_t(tp);
	char        date[32];
	struct tm   tm;
	localtime_s(&tm, &t);
	strftime(date, sizeof(date), "%Y-%m-%d %H:%M:%S", &tm);
	
	return string(date) + "." + to_string(timestamp.count() % 1000);
}

/* Nicolò:
 * returns the millisecond count since 1970
 */
std::chrono::milliseconds Position::parse_to_timestamp(std::string datetime) {
	tm tm;
	istringstream ss(datetime);
	ss >> get_time(&tm, "%Y-%m-%d %H:%M:%S");
	tm.tm_isdst = -1;	//let the mktime check whether DST is in effect or not

	int hours = tm.tm_hour;
	int minutes = tm.tm_min;
	int seconds = tm.tm_sec;

	time_t t = mktime(&tm);
	t = t * 1000; // convert seconds->milliseconds

	int ms = atoi(datetime.substr(datetime.find('.')+1 /*decimal digits*/, 3 /*milliseconds granularity*/).c_str());
	chrono::milliseconds m(t + ms);

	return m;
}

/* Nicolò:
 * returns the human-readable version of the current datetime
 */
std::string Position::datetime_now() {
	auto		time_point = chrono::system_clock::now();
	time_t		t = chrono::system_clock::to_time_t(time_point);
	char        date[32];
	struct tm   tm;
	localtime_s(&tm, &t);
	strftime(date, sizeof(date), "%Y-%m-%d %H:%M:%S", &tm);

	chrono::milliseconds ms = chrono::duration_cast<chrono::milliseconds>(time_point.time_since_epoch());
	stringstream output;
	output << date << "." << ms.count() % 1000;

	return output.str();
}

/* Nicolò:
 * returns the number of milliseconds of the current timestamp
 */
std::chrono::milliseconds Position::timestamp_now() {
	return std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now().time_since_epoch());
}

/* Nicolò:
 * mark the packet as an hidden one (i.e. having a private MAC address)
 */
void Position::set_private_MACaddress(unsigned int sequence_number, std::string SSID, unsigned int fingerprint) {
	_is_local = true;
	_sequence_number = sequence_number;
	_network_id = SSID;
	_hash_TLVframebody = fingerprint;
}

/* Nicolò:
 * external (friend) function that swap the content of
 * two Detection objects
 */
void swap(Position& first, Position& second) {
	std::swap(first._timestamp, second._timestamp);
	std::swap(first._device_id, second._device_id);
	std::swap(first._configuration_id, second._configuration_id);
	std::swap(first._coordinates, second._coordinates);
	std::swap(first._is_local, second._is_local);
	std::swap(first._sequence_number, second._sequence_number);
	std::swap(first._network_id, second._network_id);
	std::swap(first._hash_TLVframebody, second._hash_TLVframebody);
}


// ~-----operators overloading------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * move assignment operator
 */
Position& Position::operator= (Position that) {
	swap(*this, that);
	return *this;
}

/* Nicolò:
 * copy assignment operator
 */
Position& Position::operator= (const Position& source) {
	if (this != &source) {
		this->_timestamp = chrono::milliseconds(source._timestamp);
		this->_device_id = source._device_id;
		this->_configuration_id = source._configuration_id;
		this->_coordinates = source._coordinates;
		this->_is_local = source._is_local;
		this->_sequence_number = source._sequence_number;
		this->_network_id = source._network_id;
		this->_hash_TLVframebody = source._hash_TLVframebody;
	}
	return *this;
}


// ~-----getters and setters--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * getter for _timestamp
 */
std::chrono::milliseconds Position::timestamp() {
	return _timestamp;
}

/* Nicolò:
 * getter for _device_id
 */
std::string Position::MACaddress_device() {
	return _device_id;
}

/* Nicolò:
 * setter for _device_id
 */
void Position::update_MACaddress_device(std::string new_MACaddress) {
	_device_id = new_MACaddress;
}

/* Nicolò:
 * getter for _configuration_id
 */
std::string Position::configuration_id() {
	return _configuration_id;
}

/* Nicolò:
 * getter for _coordinates
 */
Coordinates Position::coordinates() {
	return _coordinates;
}

/* Nicolò:
 * getter for _sequence_number
 */
int Position::sequence_number() {
	return _sequence_number;
}

/* Nicolò:
 * getter for _network_id
 */
std::string Position::SSID() {
	return _network_id;
}

/* Nicolò:
 * getter for _hash_TLVframebody
 */
unsigned int Position::fingerprint() {
	return _hash_TLVframebody;
}