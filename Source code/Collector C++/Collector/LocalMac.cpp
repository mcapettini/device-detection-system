/*
	Author:			Marco Capettini
	Content:		Function to manage packets/devices with local (hidden) MAC addres.
					All threads put received packets into the BlockingQueue (so BQ have both normal packets and packets with local MAC).
					BlockingQueue will pass through Aggregator and Interpolator and then into DB.
					Every now and then (2 minutes) the socket part (server) will call this function.
					> Perform a SELECT on DB to retrieve all "local packets" of the last 5 minutes that are present into DB,
					  they are received as Position object.
					> Try to find correlations
					> Send back data (compacted), so essentially do an UPDATE on DB. This last function need as parameter:
					  new MAC, list of old MACs to be replaced with new MAC, list of timestamps related to old MACs. 

	Team members:	Matteo Fornero, Fabio Carfì, Marco Capettini, Nicolò Chiapello
 */

#include "stdafx.h"
#include "Socket.h"

using namespace std;

bool checkCorrelations(Position pkt_i, Position pkt_j);

void Socket::manageLocalMac()
{
	vector<Position> packetList;
	vector<string> initialMacList;

	std::map<string, vector<string>> mapMACsToSend;
	std::map<string, vector<chrono::milliseconds>> mapTimestampsToSend;

	// Function, provided by upper layer, that returns a list of local packets, ordered by timestamp ascendent
	packetList = Position::get_past_local_addresses();

	vector<Position>::iterator
		it_i,
		it_j = it_i,
		end = packetList.end();

	try {
		// Save current state of the packetList
		for (it_i = packetList.begin(); it_i != end; it_i++) {
			initialMacList.push_back(it_i->MACaddress_device());
		}

		for (it_i = packetList.begin(); it_i != end; it_i++) {
			for (it_j = it_i; it_j != end; it_j++) {
				if (it_i != it_j) {
					if (checkCorrelations(*it_i, *it_j)) {
						// There is correlation, update list
						it_j->update_MACaddress_device(it_i->MACaddress_device()); // Set the oldest MAC to both packets
					}
				}
			}
		}

		vector<string>::iterator it_mac;
		vector<chrono::milliseconds>::iterator it_time;

		// Check difference between actual list and initial one, save info about the differences in maps in order to properly call Nico function
		for (it_i = packetList.begin(), it_mac = initialMacList.begin(); it_i != end; it_i++, it_mac++) {
			if (it_i->MACaddress_device().compare(*it_mac) != 0) {
				mapMACsToSend[it_i->MACaddress_device()].push_back(*it_mac);
				mapTimestampsToSend[it_i->MACaddress_device()].push_back(it_i->timestamp());
			}
		}

		map<string, vector<string>>::iterator
			it_mapM,
			end_mapM;
		map<string, vector<chrono::milliseconds>>::iterator it_mapT;

		for (it_mapM = mapMACsToSend.begin(), it_mapT = mapTimestampsToSend.begin(); it_mapM != mapMACsToSend.end(); it_mapM++, it_mapT++) {
			//cout << it_mapM->first << endl;
			//cout << it_mapM->second.at(0) << endl;
			//cout << it_mapT->second.at(0).count() << endl;
			Position::update_past_local_addresses(it_mapM->first, it_mapM->second, it_mapT->second);
		}
		return;
	}
	catch (const std::exception&) {
		stringstream ss;
		ss << "Error occurred in the LocalMac function";
		Synchronizer::report_error(Synchronizer::SocketError, ss.str());
	}
}

bool checkCorrelations(Position pkt_i, Position pkt_j)
{
	bool apple = false;
	bool SNcanRestart = false;
	unsigned int SN1 = pkt_i.sequence_number();
	unsigned int SN2 = pkt_j.sequence_number();
	std::chrono::milliseconds TS1 = pkt_i.timestamp();
	std::chrono::milliseconds TS2 = pkt_j.timestamp();

	try {
		/*
		Check if the first 6 char (8 if counting also ":") are equal to da:a1:19 (Android/Google).
		If so, the other packet must be da:a1:19 in order to be correlated.
		Note that I consider MAC as a string, so first 6(8) char correspond to MAC first 3 bytes (exadecimal)
		*/
		if (pkt_i.MACaddress_device().compare(0, 8, "da:a1:19", 0, 8) == 0) {
			if (pkt_j.MACaddress_device().compare(0, 8, "da:a1:19", 0, 8) != 0) {
				return false; // pkt_i is Android, pkt_j not
			}
		}
		else {
			if (pkt_j.MACaddress_device().compare(0, 8, "da:a1:19", 0, 8) == 0) {
				return false; // pkt_j is Android, pkt_i not
			}
			else {
				apple = true; // both Apple(/other)
			}
		}
		// At this point they are both Android or both Apple(/other)

		// Check fingerprint, to understand what fingerprint is and what it is used for check PR_FrameID.h and the parser() in Socket.cpp
		if (pkt_i.fingerprint() != pkt_j.fingerprint()) {
			return false;
		}

		/*
		Check if the SSID is the same. Is not very useful because devices can also send packets in sequence
		changing continuosly the SSID or even not using it at all.
		However I noticed that when Apple devices with local MAC change SSID they often reset the sequence number,
		so if we have two Apple packets here and the SSID is changed, set a flag useful for sequence number analysis.
		(Is the same thing valid for Android too? It seems not)
		*/
		if (pkt_i.SSID().compare(pkt_j.SSID()) != 0 && apple == true) {
			SNcanRestart = true;
		}

		/*
		Check Timestamp overlapping: if the Timestamps are overlaid (< 1s) and the MACs aren't exactly the same, they can't be of the same device,
		because a device change the local MAC every few seconds, so if the difference is < 1sec the MAC must be still the same.
		*/
		std::chrono::milliseconds minTime = std::chrono::milliseconds{ 1000 }; // 1 second

		// pkt_i is older, his Timestamp is smaller, his Sequence Number is smaller
		if (TS2 - TS1 < minTime) {
			if (pkt_j.MACaddress_device().compare(pkt_i.MACaddress_device()) != 0) {
				return false; // Two different MACs in a too short time interval
			}
		}

		/*
		Check Sequence Number together with Timestamp, cause SN can change in accordance with how much time has passed.
		*/
		long deltaTS_over_deltaSN = 15;
		/*
		From my file on excel (SeqNum) if you use deltaTS_over_deltaSN=15 means that you are facing this ranges:
			deltaTS		deltaSN
		1s	1000		67,59259259
		3s	3000		202,7777778
		5s	5000		337,962963
		7s	7000		473,1481481
		10s	10000		675,9259259
		20s	20000		1351,851852
		30s	30000		2027,777778
		1m	60000		4055,555556
		5m	300000		20277,77778
		*/

		if (SN2 < SN1) { // SN restarted (due to overflow or, in case of Apple, SSID change)
			SN2 += 4096;
		}
		if ((SNcanRestart == true && SN2 <= 4096) || SNcanRestart == false) { // If they SSID(apple) changed but SN isn't restarded OR if SSID(apple) did not change, do normal analysis
			if ((SN2 != SN1) && ((TS2.count() - TS1.count()) / (long)(SN2 - SN1) < 15)) { // If true: considering time elasped, SN is incremented too much!
				return false;
			}
		}
		// Else if SSID changed and SN is restarted, we can't check nothing about SN 

		/*
		Check position: in order to check position, also the Timestamp cares.
		It's important to know that this check is useful only in the case that there are different devices that for extreme coincidence use local MAC
		(both equal if Android), same fingerprints, Timestamps that differ little, similar Sequence Number, but they are in position mutually distant into the room.
		In the unlucky case that they ALSO are in the same position we can't determinate that they are different device, but it's extremely unlikely.

		-If the difference of Timestamp is t<1 sec, the device cannot have travelled more than 10 meters (or maybe cannot be the same device)
		-If the difference of Timestamp is 1<t<5 sec, the device cannot have travelled more than 50 meters
		-If the difference of Timestamp is 5<t<10 sec, the device cannot have travelled more than 100 meters
		-If the difference of Timestamp is t>10 sec, the position doesn't give us any usefull information
		PARAMETERS TO BE TESTED
		*/
		std::chrono::milliseconds sec1 = std::chrono::milliseconds{ 1000 };
		std::chrono::milliseconds sec5 = std::chrono::milliseconds{ 5000 };
		std::chrono::milliseconds sec10 = std::chrono::milliseconds{ 10000 };
		double x1, y1, x2, y2; // Suppose meters

		x1 = pkt_i.coordinates().x();
		y1 = pkt_i.coordinates().y();
		x2 = pkt_j.coordinates().x();
		y2 = pkt_i.coordinates().y();

		if (TS2 - TS1 < sec1) {
			if (sqrt(pow(x1 - x2, 2) + pow(y1 - y2, 2)) > 10)
				return false;
		}
		else if (TS2 - TS1 < sec5) {
			if (sqrt(pow(x1 - x2, 2) + pow(y1 - y2, 2)) > 50)
				return false;
		}
		else if (TS2 - TS1 < sec10) {
			if (sqrt(pow(x1 - x2, 2) + pow(y1 - y2, 2)) > 100)
				return false;
		}

		// If all test passed they are packet of the same device
		return true;
	}
	catch (const std::exception&) {
		stringstream ss;
		ss << "Error occurred in the LocalMac (checkCorrelations) function";
		Synchronizer::report_error(Synchronizer::SocketError, ss.str());
	}
}