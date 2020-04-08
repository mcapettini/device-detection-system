#pragma once

// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"



// ~-----class----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * represent the position of a device, at a given timestamp
 * used to denote the information produced by the Interpolator that will feed the DB
 */
class Position
{
protected:
	// ~-----attributes-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	/* database information:
	 * information that are needed to feed the database
	 * they come from the Interpolator module
	 */
	std::chrono::milliseconds	_timestamp;			//packet identifier
	std::string					_device_id;			//device MAC address
	std::string					_configuration_id;	//user-defined configuration (room) name
	Coordinates					_coordinates;		//interpolated location of the source device
	bool						_is_local;			//states if the device MAC address is hidden or not
	unsigned int				_sequence_number;	//order of a packet inside a communication
	std::string					_network_id;		//WiFi network identifier
	unsigned int				_hash_TLVframebody;	//represent the features of the sending device (fingerprint)

	/* database credentials:
	 * account information to access the DB
	 */
	static std::string host;
	static std::string user;
	static std::string password;

	/* concurrency-specific information:
	 * synchronization primitives needed to protect the access to the current object
	 */
	std::mutex _m;



public:
	// ~-----constructors and destructors-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	
	Position(std::chrono::milliseconds timestamp, std::string MACaddress_device, std::string configuration_id, Coordinates coord);
	Position(std::chrono::milliseconds timestamp, std::string MACaddress_device, std::string configuration_id, Coordinates coord, unsigned int sequence_number, std::string SSID, unsigned int fingerprint);

	/* Nicolò:
	 * move constructur
	 */
	Position(Position&& that);

	/* Nicolò:
	 * copy constructor
	 */
	Position(const Position& source);



	// ~-----methods----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicolò:
	 * method that takes care of inserting the information of the current position, in the DB
	 */
	void insertDB();


	/* Nicolò:
	 * selects local (hidden) addresses in the last minutes
	 */
	static std::vector<Position> get_past_local_addresses();
	

	/* Nicolò:
	 * updates local (hidden) addresses once discovered they belongs to the same device
	 */
	static void update_past_local_addresses(std::string MACaddress_new, std::vector<std::string> MACaddresses_old, std::vector<std::chrono::milliseconds> timestamps);


	/* Nicolò:
	 * returns the human readable version of timestamp
	 */
	static std::string parse_to_datetime(std::chrono::milliseconds timestamp);


	/* Nicolò:
	 * returns the millisecond count since 1970
	 */
	static std::chrono::milliseconds parse_to_timestamp(std::string datetime);

	/* Nicolò:
	 * returns the human readable version of the current datetime
	 */
	static std::string datetime_now();

	/* Nicolò:
	 * returns the number of milliseconds of the current timestamp
	 */
	static std::chrono::milliseconds timestamp_now();

	/* Nicolò:
	 * mark the packet as an hidden one (i.e. having a private MAC address)
	 */
	void set_private_MACaddress(unsigned int sequence_number, std::string SSID, unsigned int fingerprint);

	/* Nicolò:
	 * external (friend) function that swap the content of
	 * two Detection objects
	 */
	friend void swap(Position& first, Position& second);



	// ~-----operators overloading--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicolò:
	 * move assignment operator
	 */
	Position& operator= (Position that);


	/* Nicolò:
	 * copy assignment operator
	 */
	Position& operator= (const Position& source);

	
	// ~-----getters and setters----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicolò:
	 * getter for _timestamp
	 */
	std::chrono::milliseconds timestamp();

	/* Nicolò:
	 * getter for _device_id
	 */
	std::string MACaddress_device();

	/* Nicolò:
	 * setter for _device_id
	 */
	void update_MACaddress_device(std::string new_MACaddress);

	/* Nicolò:
	 * getter for _configuration_id
	 */
	std::string configuration_id();

	/* Nicolò:
	 * getter for _coordinates
	 */
	Coordinates coordinates();

	/* Nicolò:
	 * getter for _sequence_number
	 */
	int sequence_number();

	/* Nicolò:
	 * getter for _network_id
	 */
	std::string SSID();

	/* Nicolò:
	 * getter for _hash_TLVframebody
	 */
	unsigned int fingerprint();

};

