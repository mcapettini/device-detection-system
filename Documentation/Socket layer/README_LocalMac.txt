>>>>>>>>>>>>>> LocalMac.cpp
Function to manage packets/devices with local (hidden) MAC addres.

BACKGROUND:
all Socket.cpp threads put received (from ESPs) packets into the BlockingQueue (so the BQ has both normal
packets and packets with local MAC). BQ will pass through Aggregator and Interpolator and then into DB.

WORKFLOW:
every now and then (2 minutes) the server (Socket.cpp) will call this function that:
1. 	Perform a SELECT on DB to retrieve all packets/detections (tupla of the DB) with local MAC of the
	last 5 minutes (they are received as Position objects).
2.  Try to find correlations (double for loop on the list of packets).
3.  Perform an UPDATE on the DB, so essentially send back data (compacted).

1:
-To perform the SELECT call the function "get_past_local_addresses()".
-Before doing correlations save the list received, in this way when you have done the correlations you can
check which element of the list has changed and do an UPDATE only for those elements.

2:
-For every pair (packet-packet) do one check after another until you find something that proves that those
two packets are different (return false), if you don't find anything it means that you have to consider
those two packets correlated (return true).
-For information about the checks look at the comments into the code in function "checkCorrelations()".

3:
-Check difference between actual list (with correlation) and initial one, to do properly the UPDATEs
-To perform the UPDATE call "update_past_local_addresses(...)"
-The UPDATE will update the MAC of ALL correlated packet applying the oldest MAC to all of them.
ex:
	before update: packets in the DB with MAC A, B, C, D, E
	packets A is correlated with D and with E
	packets B is correlated with C
	after update: those packets in the DB will have these MACs: A, B, B, A, A 

NOTES:
From my correlation analysis if two packets are not correlated it means that you have evidence that
those packets belongs to different phones. Otherwise if two packets are correlated means that you have
not found any significant prove that they belongs to different phones. 
