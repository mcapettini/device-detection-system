>>>>>>>>>>>>>> Socket.cpp
BACKGROUND:
The server collects data (packets) from all ESPs and provides them to upper layers.
Packets are received by the server coming from the ESP in this format:
-"DATA" (string)
-Lenght of the packet
-MAC address
-RSSI (signal intensity)
-Timestamp (milliseconds)
-Sequence number
-Hash
-TLV structure / frame body

The TLV structure is a part of the packet that contains a lot of additional informations, which in our
case is a subset of all the information present in the real TLV structure of the WIFI packet (i.e. the
so called "Frame Body") sniffed by the ESP.

The server can operate in two mode, if autoSocket=0 the "regular" server is used, if autoSocket=1 the
"regular" server will call a function called "startAutoServer()" that does some work and then return
to the "regular" server, from this point onwards it proceed the "regular" server.
Just for clarity, if the "regular" server call that function, we call it "autoServer"  
-autoServer: is called when the user choose the automatic setup of the system (autoSocket=1):
			 provides APIs to make the ESP blink or SLEEP or proceed with regular behaviour.
-Server: is the regular server.

WORKFLOW:
1. Some procedures to start Winsock properly and set the main TCP socket(s)
2. Start a pseudo-infinite loop (stops when "somenone" tells to stop or some error occurs)
	3. Check if have to call autoServer (if yes go to 4.1).
	4. Check if have to call manageLocalMac() now or next cycle (see LocalMac.cpp).
	5. For-loop to establish N_ESP connections/sockets
		6. Check if sockets must be new (perform an "accept") or can be old (re-uses the sockets of
		   recAndPut() if possible), and this is done exchanging "ALIVE" messages.
		7. Receive MAC from ESP and tell to go to SYNCH if the MAC is ok, or to SLEEP if the MAC is not ok
		8. Receive a message from ESP that tells if the SYNCH was done correctly (NOSYNC: ESP was able to
			get the time from NTP server) or has to be done manually (SYNREQ: the ESP request manual synch)
		9. If has to be done manually (SYNREQ), send a bunch of timestamps.
	10. ESP are ready and so the server, so tell ESP to start sniffing: send of GO\0 and close sockets.
	--- a minute later
	11. For-loop to accept N_ESP connections, which means that N_ESP are ready to send data.
	12. Create N_ESP threads each one doing the function recAndPut() (go to 12.1)
	13. Thread father waits all the threads.
	14. Repeat: go to 4.
15. Stop.

4: 
4.1 Perform an "accept" after another until an accept waits more then "timeout" seconds, if so it means
	that all the surrounding ESPs has performed an accept and no more ESP are trying to connect (because
	you already have accepted them all).	
 .2 Receive MAC from all the ESP.
 .3 Send all these MAC to GUI, then wait for GUI instructions (enter "GUI_command_routine()" and waits).
 .4 If GUI want to blink an ESP (particular MAC), then send a BLINK message to that ESP.
 .5	If GUI started the system a list of chosen MAC is received, so then send a SLEEP message to all the
	ESP with a non-chosen MAC and a SYNCH message to all the ESP with a chosen MAC.
 .6 Perform synchronization (like steps 8. and 9.)
 .7 Return to regular server, but pay attention: not to step 4. but directly to step 10. because synchronization
	has already been done!
 
12:
12.1 Receive ESP MAC address (together with a message: SENDING)
  .2 Read number of packets to receive and loop until you haven't received all of them
	  .3 Receive "DATA" string
	  .4 Receive lenght of packet
	  .5 Receive packet (if you receive less than packet lenght receive again until the end)
	  .6 Deserialize and extract fingerprint from frame body calling "parser()"
		 (If you want additional information on how parser works, there are comment inside the parser
		 function itself).
	  .7 Put in Blocking Queue
  .8 Record the socket and return.





























