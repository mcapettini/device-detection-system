//	ESP32 Probe Request Analyzer
//	Team members: Matteo Fornero, Fabio Carfì, Marco Capettini, Nicolò Chiapello
//	Author: Fornero Matteo 

//#include "pch.h"
#include "stdafx.h"

#include "ESPpacket.h"

using namespace std;

ESPpacket::ESPpacket()
{
	rssi = 0;
	timestamp = 0;
	//pktlen = 0;
	//channel = 0;
	seq_number = 0;
	hash = 0;
	src_MAC = "\0";
	//payload = NULL;
}

ESPpacket::ESPpacket(int8_t sigstrength, uint64_t time, /*uint16_t len, uint8_t chann,*/ uint16_t seq, uint32_t crc32, std::string sourceMAC/*, char *body*/)
{
	rssi = sigstrength;
	timestamp = time;
	//pktlen = len;
	//channel = chann;
	seq_number = seq;
	hash = crc32;
	src_MAC = sourceMAC;
	//payload = body;
}

ESPpacket::~ESPpacket()
{
	/*if (payload != NULL) {
		free(payload); // free the memory previously allocated
	}*/
}

string ESPpacket::get_MAC()
{
	return src_MAC;
}

uint64_t ESPpacket::get_timestamp()
{
	return timestamp;
}

uint16_t ESPpacket::get_seqnum()
{
	return seq_number;
}

uint32_t ESPpacket::get_hash()
{
	return hash;
}

int8_t ESPpacket::get_rssi()
{
	return rssi;
}

/*
char* ESPpacket::getpayload()
{
	return payload;
}

uint16_t ESPpacket::getdim()
{
	return pktlen + STDLEN + 6; // length of metadata + dumped payload + TL values (type and length)
}

uint16_t ESPpacket::getpktlen()
{
	return pktlen;
}
*/

void serialize_uint(unsigned char *buffer, uint64_t value, uint8_t len)
{
	// note that this approach is platform independent (don't care about hardware specs, endianness etc...)
	uint8_t i;
	for (i = 0; i < len; i++) {
		buffer[i] = value >> ((8 * (len - 1)) - (8 * i));
	}
}

uint64_t deserialize_uint(unsigned char *buffer, uint8_t len)
{
	uint64_t value = 0;
	for (uint8_t i = 0; i < len; i++) {
		value += buffer[i];
		if (i != len - 1) {
			value = value << 8;
		}
	}
	return value;
}

ESPpacket deserialize_data(char *buffer)
{
	std::string _source(buffer, MAC_DIM-1);
	buffer += MAC_DIM-1;
	int8_t _rssi = (int8_t)buffer[0];
	buffer++;
	uint64_t _timestamp = deserialize_uint((unsigned char*)buffer, 8);
	buffer += 8;
	uint16_t _seq_num = deserialize_uint((unsigned char*)buffer, 2);
	buffer += 2;
	uint32_t _fcs = deserialize_uint((unsigned char*)buffer, 4);
	buffer += 4;
	ESPpacket pkt(_rssi, _timestamp, _seq_num, _fcs, _source);
	return pkt;
}