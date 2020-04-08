/*
	Author:			Marco Capettini
	Content:		SOCKET PART
					Server that collects data from all ESPs and provides them to upper layers

	Team members:	Matteo Fornero, Fabio Carfì, Marco Capettini, Nicolò Chiapello
 */

#pragma once

#undef UNICODE

#define WIN32_LEAN_AND_MEAN // To speed the build process: reduce the size of the Win32 header files

#include <windows.h>
#include <winsock2.h>
#include <ws2tcpip.h> // To use functionalities of Winsock2
#include <stdlib.h>
#include <stdio.h>
#include <iostream>
#include <thread>
#include <vector>
#include <fstream>
#include <algorithm>
#include <atomic>

#include "ESPpacket.h"
#include "Detection.h"
#include "BlockingQueue_Aggregator.h"
#include "PR_FrameIDs.h"
#include "Synchronizer.h"

#pragma comment (lib, "Ws2_32.lib") // Better than manually include the library in linker's input dependecies

#define DEFAULT_PORT "1500" // Server port

using namespace std;

class Socket
{
protected:

	// ATTRIBUTES
	shared_ptr<BlockingQueue_Aggregator> BQ_A_ptr; // Pointer to the Blocking Queue of the Aggregator
	int N_ESP; // Number of ESPs
	vector<string> boards_mac;
	int autoSocket;
	bool localMacClock = false; // If true call manageLocalMac()
	SOCKET ListenSocket; // Passive socket
	vector<SOCKET> s; // Vector of sockets used by threads to comunicate with ESPs, and if possible reused for synchronization
	vector<SOCKET> autoS;
	vector<SOCKET> s_ESPSocket; // Vector of copies of ESPSocket, each thread has an ESPSocket to receive data

public:
	// CONSTRUCTOR
	Socket(int N_ESP, vector<string> boards_mac, shared_ptr<BlockingQueue_Aggregator> BQ_A_ptr);
	Socket();

	// FUNCTIONAL OBJECT
	void operator() ();

	// OTHER METHODS
	/*
		Entry point of the server, the very start of all the system.
	
		"__cdecl" is a calling convenction: when you call a function, what happens at the assembly level is all the passed-in parameters are pushed to the stack,
		then the program jumps to a different area of code. The new area of code looks at the stack and expects the parameters to be placed there.
		Different calling conventions push parameters in different ways. "__cdecl" is the default calling convention for C and C++ programs.
		It permits to create larger executables than "__stdcall", because it requires each function call to include stack cleanup code.
	*/
	int __cdecl startServer();

	/*
		Exit point of the server, it is used by upper layers. The function that permits to stop the application by calling stopRoutine() through a notify().
	*/
	void stopServer();

	/*
		An alternative entry point of the server that collects information about all the ESP in the nearing and show them to the users,
		who can decide to make some board light up to help the positioning.
		It can be called on the beginning of startServer.
	*/
	int serverAutoConfig();

	/*
		The real "exit-procedure" done by a socket's thread, after upper layer has called stopServer().
		This function manage "s_ESPSocket" and "s" vectors, and "ListenSocket" to properly stop the server application.
		In fact we close all the socket to make the server aware of the stop.
		In particular is necessary because while we are in concurrency (recAndPut function) each thread has it's own open socket and we need a copy
		of those socket in order to permits someone (stopServer function) to immediately stop the application and avoid waiting all thread to return.
	*/
	void stopRoutine();

	/*
		This is the function used to receive data from ESP. Receives and deserializes every single packet, and finally call the function putInBQ.
		This function is executed in parallel by different threads, in order to receive data from different ESPs at the same time.
	*/
	void recAndPut(int index);

	/*
		This function takes as input the frame body of each packet, search for only some recurrent elements and if it finds something interesting
		it will concatenates the content (or only the ID) to a string. Finally the string is hashed and returned in order to provide a "fingerprint"
		of that particular packet. This function also return the SSID as a separate variable.
	*/
	uint32_t parser(unsigned char *buffer, size_t len, string *SSID);

	/*	
		Simply convert packets from "ESPpacket" to "Detection" objects and put them into the Blockin Queue.
	*/
	void putInBQ(ESPpacket pkt, string macBoard, string SSID, uint32_t fingerprint);

	/*
		Each thread puts every received packet into the (same) Blocking Queue (which therefore will contain packets with local MAC and normal packets),
		The BQ will pass through Aggregator and Interpolator and finally the data will be uploaded into the DB.
		Every now and then (2 minutes) the server will call this function.
		It performs a SELECT on DB to retrieve local packets of the last 5 minutes, it tries to find correlations and finally UPDATEs the DB with new informations.
	*/
	void manageLocalMac();

	/*
		Called after having sent the list of heard MACs to the GUI.
		The thread goes to wait/sleep until a command of BLINK (Blink) or SYNCH/SLEEP (concludeAutoSetup) is sent by the GUI,
		which is taken and sent to the right ESP. When the GUI say "start" (command of SYNCH/SLEEP) this function sends the commands and exits,
		after having returned to startServer (the real server) 
	*/
	int GUI_command_routine(vector<string> MACs);

	/*
		Permits the GUI to blink the led of the ESP related to "MAC"
	*/
	void Blink(string MAC);

	/*
		Called by the GUI when the user has decided which MACs will be used. This list of MACs is taken and passed to GUI_command_routine,
		and will also be used (together with the other parameters) by the real server.
	*/
	void concludeAutoSetup(int N_ESP, vector<string> boards_mac, shared_ptr<BlockingQueue_Aggregator> BQ_A_ptr);
};

class smart_ptr {
	unsigned int size;
	int shift;
	char* ptr;
	
public:
	/*
		If a class has a constructor which can be called with a single argument, then this constructor becomes conversion constructor
		because such a constructor allows conversion of the single argument to the class being constructed.
		We can avoid such implicit conversions as these may lead to unexpected results. We can make the constructor explicit with the help of explicit keyword.
		See https://www.geeksforgeeks.org/g-fact-93/
	*/
	// Constructur
	explicit smart_ptr(unsigned int size) {
		this->size = size;
		this->shift = 0;
		ptr = new char[size];
	}

	// Copy constructur
	smart_ptr(const smart_ptr& source);

	// Assignment operator
	smart_ptr& operator= (const smart_ptr& source);

	// Destructor
	~smart_ptr() {
		if (this->shift > 0) {
			this->ptr = this->ptr - this->shift;
			this->shift = 0;
		}
		else if (this->shift < 0) {
			this->ptr = this->ptr + this->shift;
			this->shift = 0;
		}
		delete[] ptr;
	}

	char* get() { return this->ptr;	}

	// Operator overloading
	char& operator*() { return *ptr; }

	char* operator->() { return ptr; }

	void operator + (int index) {	
		this->ptr = this->ptr + index;
		this->shift += index;
	}

	void operator - (int index) {
		this->ptr = this->ptr - index;
		this->shift -= index;
	}
};