// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include "Packet.h"



// ~-----constructors and destructors-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Packet::Packet()
{
}


Packet::Packet(
	unsigned int hash,
	std::string MACaddress_device)
{
	this->_hash_packet = hash;
	this->_device_id = MACaddress_device;
	this->_is_local = false;
	this->_sequence_number = 0;
	this->_network_id = "";
	this->_hash_TLVframebody = 0;
}


Packet::Packet(
	unsigned int hash,
	std::string MACaddress_device,
	unsigned int sequence_number,
	std::string SSID,
	unsigned int fingerprint)
{
	this->_hash_packet = hash;
	this->_device_id = MACaddress_device;
	this->_is_local = true;
	this->_sequence_number = sequence_number;
	this->_network_id = SSID;
	this->_hash_TLVframebody = fingerprint;
}

/* Nicolò:
 * move constructur
 */
Packet::Packet(Packet&& that) {
	swap(*this, that);
}

/* Nicolò:
 * copy constructur
 */
Packet::Packet(const Packet& source) {
	this->_hash_packet = source._hash_packet;
	this->_device_id = source._device_id;
	this->_is_local = source._is_local;
	this->_sequence_number = source._sequence_number;
	this->_network_id = source._network_id;
	this->_hash_TLVframebody = source._hash_TLVframebody;
	for (auto iter = source._signal_powers.begin(); iter != source._signal_powers.end(); iter++) {
		this->_signal_powers = source._signal_powers;
	}
	for (auto iter = source._timestamps.begin(); iter != source._timestamps.end(); iter++) {
		this->_timestamps = source._timestamps;
	}
}



// ~-----methods--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * allow to insert the information of a Detection in a Packet object
 */
void Packet::add(std::string MACaddress_board, std::chrono::milliseconds timestamp, double RSSI) {
	// ~-----local variables-----------------------------
	std::lock_guard<std::mutex> lg(_m);

	// ~-----insert in the maps--------------------------
	_timestamps[MACaddress_board] = timestamp;
	_signal_powers[MACaddress_board] = RSSI;
}


/* Nicolò:
* retrieve the information about detection time, related to the specified board
*/
std::chrono::milliseconds Packet::get_timestamp(std::string MACaddress_board) {
	// ~-----local variables-----------------------------
	std::lock_guard<std::mutex> lg(_m);

	// ~-----check if information exists-----------------
	if (_timestamps.find(MACaddress_board) == _timestamps.end())
		throw std::exception("The requested board has not detected the current packet");

	// ~-----retrieve the value, given the key-----------
	return _timestamps[MACaddress_board];
}


/* Nicolò:
* retrieve the information about signal power, related to the specified board
*/
double Packet::get_RSSI(std::string MACaddress_board) {
	// ~-----local variables-----------------------------
	std::lock_guard<std::mutex> lg(_m);

	// ~-----check if information exists-----------------
	if (_signal_powers.find(MACaddress_board) == _signal_powers.end())
		throw std::exception("The requested board has not detected the current packet");

	// ~-----retrieve the value, given the key-----------
	return _signal_powers[MACaddress_board];
}

/* Nicolò:
 * mark the packet as an hidden one (i.e. having a private MAC address)
 */
void Packet::set_private_MACaddress(unsigned int sequence_number, std::string SSID, unsigned int fingerprint) {
	this->_is_local = true;
	this->_sequence_number = sequence_number;
	this->_network_id = SSID;
	this->_hash_TLVframebody = fingerprint;
}

/* Nicolò:
 * check if the object has no attributes (standard costructor)
 * used by the BlockingQueue_Interpolator to notify the termination of the engine
 */
bool Packet::empty() {
	return (_hash_packet == 0 && _device_id.empty()
		&& _signal_powers.empty() && _timestamps.empty());
}

/* Nicolò:
 * states if the original packet has an hidden MAC address
 */
bool Packet::is_private() {
	return _is_local;
}

/* Nicolò:
 * external (friend) function that swap the content of
 * two Packet objects
 */
void swap(Packet& first, Packet& second) {
	std::swap(first._hash_packet, second._hash_packet);
	std::swap(first._device_id, second._device_id);
	std::swap(first._is_local, second._is_local);
	std::swap(first._sequence_number, second._sequence_number);
	std::swap(first._network_id, second._network_id);
	std::swap(first._hash_TLVframebody, second._hash_TLVframebody);
	std::swap(first._signal_powers, second._signal_powers);
	std::swap(first._timestamps, second._timestamps);
}


// ~-----operators overloading------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * move assignment operator
 */
Packet& Packet::operator= (Packet that) {
	swap(*this, that);
	return *this;
}



// ~-----getters and setters--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * getter for _hash_packet
 */
unsigned int Packet::hash() {
	return _hash_packet;
}

/* Nicolò:
 * getter for _device_id
 */
std::string Packet::MACaddress_device() {
	return _device_id;
}

/* Nicolò:
 * getter for _sequence_number
 */
int Packet::sequence_number() {
	return _sequence_number;
}

/* Nicolò:
 * getter for _network_id
 */
std::string Packet::SSID() {
	return _network_id;
}

/* Nicolò:
 * getter for _hash_TLVframebody
 */
unsigned int Packet::fingerprint() {
	return _hash_TLVframebody;
}

/* Nicolò:
 * getter for _signal_powers
 */
std::map<std::string, int> Packet::RSSIs() {
	return _signal_powers;
}

/* Nicolò:
 * getter for _timestamps
 */
std::map<std::string, std::chrono::milliseconds> Packet::timestamps() {
	return _timestamps;
}