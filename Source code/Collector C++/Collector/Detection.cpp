// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include "Detection.h"


// ~-----namespaces-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
using namespace std;



// ~-----constructors and destructors-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Detection::Detection()
{
}


Detection::Detection(
	unsigned int hash,
	std::string MACaddress_board,
	std::string MACaddress_device,
	std::chrono::milliseconds timestamp,
	int RSSI)
{
	this->_hash_packet = hash;
	this->_board_id = MACaddress_board;
	this->_device_id = MACaddress_device;
	this->_timestamp = timestamp;
	this->_signal_power = RSSI;
	this->_is_local = false;
	this->_sequence_number = 0;
	this->_network_id = "";
	this->_hash_TLVframebody = 0;
}


Detection::Detection(
	unsigned int hash,
	std::string MACaddress_board,
	std::string MACaddress_device,
	std::chrono::milliseconds timestamp,
	int RSSI,
	unsigned int sequence_number,
	std::string SSID,
	unsigned int fingerprint)
{
	this->_hash_packet = hash;
	this->_board_id = MACaddress_board;
	this->_device_id = MACaddress_device;
	this->_timestamp = timestamp;
	this->_signal_power = RSSI;
	this->_is_local = true;
	this->_sequence_number = sequence_number;
	this->_network_id = SSID;
	this->_hash_TLVframebody = fingerprint;
}

/* Nicolò:
 * move constructur
 */
Detection::Detection(Detection&& that) {
	swap(*this, that);
}

/* Nicolò:
 * copy constructur
 */
Detection::Detection(const Detection& source) {
	this->_hash_packet = source._hash_packet;
	this->_board_id = source._board_id;
	this->_device_id = source._device_id;
	this->_timestamp = source._timestamp;
	this->_signal_power = source._signal_power;
	this->_is_local = source._is_local;
	this->_sequence_number = source._sequence_number;
	this->_network_id = source._network_id;
	this->_hash_TLVframebody = source._hash_TLVframebody;
}


// ~-----methods--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * external (friend) function that swap the content of
 * two Detection objects
 */
void swap(Detection& first, Detection& second) {
	std::swap(first._hash_packet, second._hash_packet);
	std::swap(first._board_id, second._board_id);
	std::swap(first._device_id, second._device_id);
	std::swap(first._timestamp, second._timestamp);
	std::swap(first._signal_power, second._signal_power);
	std::swap(first._is_local, second._is_local);
	std::swap(first._sequence_number, second._sequence_number);
	std::swap(first._network_id, second._network_id);
	std::swap(first._hash_TLVframebody, second._hash_TLVframebody);
}

/* Nicolò:
 * mark the packet as an hidden one (i.e. having a private MAC address)
 */
void Detection::set_private_MACaddress(unsigned int sequence_number, std::string SSID, unsigned int fingerprint) {
	this->_is_local = true;
	this->_sequence_number = sequence_number;
	this->_network_id = SSID;
	this->_hash_TLVframebody = fingerprint;
}

/* Nicolò:
 * states if the original packet has an hidden MAC address
 */
bool Detection::is_private() {
	return _is_local;
}


// ~-----operators overloading------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * move assignment operator
 */
Detection& Detection::operator= (Detection that) {
	swap(*this, that);
	return *this;
}

/* Nicolò:
 * copy assignment operator
 */
/*Detection& Detection::operator= (const Detection& source) {
	if (this != &source) {
		this->_hash_packet, source._hash_packet;
		this->_board_id, source._board_id;
		this->_device_id, source._device_id;
		this->_timestamp, source._timestamp;
		this->_signal_power, source._signal_power;
		this->_sequence_number, source._sequence_number;
		this->_network_id, source._network_id;
	}
	return *this;
}*/

/* Nicolò:
 * ordering operator
 */
bool Detection::operator< (const Detection& d) const {
	if (this->_hash_packet < d._hash_packet) {
		return true;
	}
	else if (this->_hash_packet > d._hash_packet) {
		return false;
	}
	else {
		if (this->_board_id < d._board_id)
			return true;
		else
			return false;
	}
}

/* Nicolò:
 * comparator operator
 */
bool Detection::operator== (const Detection& d) const {
	return (this->_hash_packet == d._hash_packet
		&& this->_board_id == d._board_id
		&& this->_device_id == d._device_id
		&& this->_timestamp == d._timestamp
		&& this->_signal_power == d._signal_power
		&& this->_is_local == d._is_local
		&& this->_sequence_number == d._sequence_number
		&& this->_network_id == d._network_id
		&& this->_hash_TLVframebody == d._hash_TLVframebody);
}



// ~-----getters and setters--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * getter for _hash_packet
 */
unsigned int Detection::hash() {
	return _hash_packet;
}

/* Nicolò:
 * getter for _board_id
 */
std::string Detection::MACaddress_board() {
	return _board_id;
}

/* Nicolò:
 * getter for _device_id
 */
std::string Detection::MACaddress_device() {
	return _device_id;
}

/* Nicolò:
 * getter for _timestamp
 */
std::chrono::milliseconds Detection::timestamp() {
	return _timestamp;
}

/* Nicolò:
 * getter for _signal_power
 */
int Detection::RSSI() {
	return _signal_power;
}

/* Nicolò:
 * getter for _sequence_number
 */
unsigned int Detection::sequence_number() {
	return _sequence_number;
}

/* Nicolò:
 * getter for _network_id
 */
std::string Detection::SSID() {
	return _network_id;
}

/* Nicolò:
 * getter for _hash_TLVframebody
 */
unsigned int Detection::fingerprint() {
	return _hash_TLVframebody;
}