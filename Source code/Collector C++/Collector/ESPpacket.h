#pragma once

/*  ESP32 Probe Request Analyzer
Team members: Matteo Fornero, Fabio Carf�, Marco Capettini, Nicol� Chiapello
Author: Fornero Matteo */

#include <string>
#include <time.h>

#define STDLEN 36 // length of metadata

/****************************************************************/

class ESPpacket
{
	// length of serialized data to send them over TCP conn. = 1 + 8 + 2 + 1 + 2 + 4 + 18 + length of dump
private:
	int8_t rssi; // signal strength
	uint64_t timestamp; // time since 1/1/1970
	//uint16_t pktlen; // length of data in TLV mode (this is the content of L field)
	//uint8_t channel; // capture channel	
	uint16_t seq_number; // sequence number of probe request
	uint32_t hash; // frame check sequence of probe request, use it as hash
	std::string src_MAC; // source MAC address
	//char *payload; // pointer to the memory location where the dump of the packet was copied
public:
	ESPpacket();
	ESPpacket(int8_t sigstrength, uint64_t time, /*uint16_t len, uint8_t chann,*/ uint16_t seq, uint32_t crc32, std::string sourceMAC/*, char *body*/);
	~ESPpacket();
	std::string get_MAC();
	uint64_t get_timestamp();
	uint16_t get_seqnum();
	uint32_t get_hash();
	int8_t get_rssi();
	/*
	char *getpayload();
	uint16_t getdim();
	uint16_t getpktlen();
	*/
};

/****************************************************************/

#define SECONDS 1000 // convert microseconds to seconds
#define MAC_DIM 18
#define SSID_DIM 33
#define CHANNELS 13

/****************************************************************/

// functions to serialize packet info before sending through the socket
void serialize_uint(unsigned char *buffer, uint64_t value, uint8_t len);
uint64_t deserialize_uint(unsigned char *buffer, uint8_t len);
ESPpacket deserialize_data(char *buffer);

int gettimeofday(struct timeval *tp);