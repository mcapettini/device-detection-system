#pragma once

// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"



// ~-----class----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicol�:
 * represent a the detection of a device, in the predisposed environment
 * used to represent the information that a board (ESP32) sent to the collector (PC)
 */
class Detection
{
protected:
	// ~-----attributes-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	unsigned int				_hash_packet;		//packet identifier
	std::string					_board_id;			//board MAC address
	std::string					_device_id;			//device MAC address
	std::chrono::milliseconds	_timestamp;			//detection time
	int							_signal_power;		//received signal strength indicator (RSSI)
	bool						_is_local;			//states if the device MAC address is hidden or not
	unsigned int				_sequence_number;	//order of a packet inside a communication
	std::string					_network_id;		//WiFi network identifier (SSID)
	unsigned int				_hash_TLVframebody;	//represent the features of the sending device (fingerprint)



public:
	// ~-----constructors and destructors-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	Detection();
	Detection(unsigned int hash, std::string MACaddress_board, std::string MACaddress_device, std::chrono::milliseconds timestamp, int RSSI);
	Detection(unsigned int hash, std::string MACaddress_board, std::string MACaddress_device, std::chrono::milliseconds timestamp, int RSSI, unsigned int sequence_number, std::string SSID, unsigned int fingerprint);
	
	/* Nicol�:
	 * move constructur
	 */
	Detection(Detection&& that);

	/* Nicol�:
	 * copy constructur
	 */
	Detection(const Detection& source);



	// ~-----methods----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicol�:
	 * external (friend) function that swap the content of
	 * two Detection objects
	 */
	friend void swap(Detection& first, Detection& second);

	/* Nicol�:
	 * mark the packet as an hidden one (i.e. having a private MAC address)
	 */
	void set_private_MACaddress(unsigned int sequence_number, std::string SSID, unsigned int fingerprint);

	/* Nicol�:
	 * states if the original packet has an hidden MAC address
	 */
	bool is_private();


	// ~-----operators overloading--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicol�:
	 * move assignment operator
	 */
	Detection& operator= (Detection that);

	/* Nicol�:
	 * copy assignment operator
	 */
	//Detection& operator= (const Detection& source);

	/* Nicol�:
	 * ordering operator
	 */
	bool operator< (const Detection& d) const;

	/* Nicol�:
	 * comparator operator
	 */
	bool operator== (const Detection& d) const;


	// ~-----getters and setters----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicol�:
	 * getter for _hash_packet
	 */
	unsigned int hash();

	/* Nicol�:
	 * getter for _board_id
	 */
	std::string MACaddress_board();

	/* Nicol�:
	 * getter for _device_id
	 */
	std::string MACaddress_device();

	/* Nicol�:
	 * getter for _timestamp
	 */
	std::chrono::milliseconds timestamp();

	/* Nicol�:
	 * getter for _signal_power
	 */
	int RSSI();

	/* Nicol�:
	 * getter for _sequence_number
	 */
	unsigned int sequence_number();

	/* Nicol�:
	 * getter for _network_id
	 */
	std::string SSID();

	/* Nicol�:
	 * getter for _hash_TLVframebody
	 */
	unsigned int fingerprint();
};

