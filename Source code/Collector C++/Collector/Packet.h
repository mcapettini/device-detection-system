#pragma once

// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"



// ~-----class----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicol�:
* represent a packet detected by all the boards
* used to denote the information  produced by the Aggregator and consumed by the Interpolator
*/
class Packet
{
protected:
	// ~-----attributes-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	/* common information:
	 * information that are common among all different related Detection
	 * they coming directly from the original packet
	 */
	unsigned int	_hash_packet;		//packet identifier
	std::string		_device_id;			//device MAC address
	bool			_is_local;			//states if the device MAC address is hidden or not
	unsigned int	_sequence_number;	//order of a packet inside a communication
	std::string		_network_id;		//WiFi network identifier
	unsigned int	_hash_TLVframebody;	//represent the features of the sending device (fingerprint)
	
	/* detection-specific information:
	 * information that are specific for each different related Detection
	 * they are attached by each different board
	 * Their storing schema has the following fields:
	 *    - key: board ID (board MAC address)
	 *    - value: signal power (RSSI) or detection time (timestamp)
	 */
	std::map<std::string, int>							_signal_powers;
	std::map<std::string, std::chrono::milliseconds>	_timestamps;

	/* concurrency-specific information:
	 * synchronization primitives needed to protect the access to the current object
	 */
	std::mutex _m;


public:
	// ~-----constructors and destructors-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	Packet();
	Packet(unsigned int hash, std::string MACaddress_device);
	Packet(unsigned int hash, std::string MACaddress_device, unsigned int sequence_number, std::string SSID, unsigned int fingerprint);

	/* Nicol�:
	 * move constructur
	 */
	Packet(Packet&& that);

	/* Nicol�:
	 * copy constructur
	 */
	Packet(const Packet& source);


	// ~-----methods----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicol�:
	* allow to insert the information of a Detection in a Packet object
	*/
	void add(std::string MACaddress_board, std::chrono::milliseconds timestamp, double RSSI);

	/* Nicol�:
	* retrieve the information about detection time, related to the specified board
	*/
	std::chrono::milliseconds get_timestamp(std::string MACaddress_board);

	/* Nicol�:
	* retrieve the information about signal power, related to the specified board
	*/
	double get_RSSI(std::string MACaddress_board);

	/* Nicol�:
	 * mark the packet as an hidden one (i.e. having a private MAC address)
	 */
	void set_private_MACaddress(unsigned int sequence_number, std::string SSID, unsigned int fingerprint);

	/* Nicol�:
	 * check if the object has no attributes (standard costructor)
	 * used by the BlockingQueue_Interpolator to notify the termination of the engine
	 */
	bool empty();

	/* Nicol�:
	 * states if the original packet has an hidden MAC address
	 */
	bool is_private();

	/* Nicol�:
	 * external (friend) function that swap the content of
	 * two Packet objects
	 */
	friend void swap(Packet& first, Packet& second);



	// ~-----operators overloading--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicol�:
	 * move assignment operator
	 */
	Packet& operator= (Packet that);



	// ~-----getters and setters----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicol�:
	 * getter for _hash_packet
	 */
	unsigned int hash();

	/* Nicol�:
	 * getter for _device_id
	 */
	std::string MACaddress_device();

	/* Nicol�:
	 * getter for _sequence_number
	 */
	int sequence_number();

	/* Nicol�:
	 * getter for _network_id
	 */
	std::string SSID();

	/* Nicol�:
	 * getter for _hash_TLVframebody
	 */
	unsigned int fingerprint();

	/* Nicol�:
	 * getter for _signal_powers
	 */
	std::map<std::string, int> RSSIs();

	/* Nicol�:
	 * getter for _timestamps
	 */
	std::map<std::string, std::chrono::milliseconds> timestamps();
};

