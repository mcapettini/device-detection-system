/*
	Author:			Marco Capettini, Matteo Fornero
	Content:		This file contains information about TLV entry of Probe Request (PR) packets (in our case sent by ESPs)
					In particular here we have definitions about the ElementID of each TLV entry in 802.11 Management Frame Body.
					This is not a complete list of all elementID specified by 802.11-2016 but it contains only IDs that
					could be found in probe request frames.
					Note that the VHT elementID was introduced in 802.11-2013 and is related to 802.11ac. This standard is
					not supported by the WiFi module of the ESP32 but VHT elementID was added for sake of completeness.

	Team members:	Matteo Fornero, Fabio Carfì, Marco Capettini, Nicolò Chiapello
 */

#define IE_SSID                                 0
#define IE_SUPPORTED_RATES                      1
#define IE_DSSS_PARAMETER_SET                   3
#define IE_REQUEST                              10
#define IE_HT_CAPABILITIES                      45
#define IE_EXTENDED_SUPPORTED_RATES             50
#define IE_SUPPORTED_OPERATING_CLASSES          59
#define IE_20_40_BSS_COEXISTENCE                72
#define IE_SSID_LIST                            84
#define IE_CHANNEL_USAGE                        97
#define IE_INTERWORKING                         107
#define IE_MESH_ID                              114
#define IE_EXTENDED_CAPABILITIES                127
#define IE_VHT_CAPABILITIES                     191
#define IE_VENDOR_SPECIFIC                      221

/*
	SSID:
	Same device can send packet with different SSID so is not very useful, however I figured out that when SSID changes
	Seq Num restart too in Apple device, this can be in some way helpfull.
	Need to be taken and copied into packet.

	SUPPORTED RATES:
	Every byte it's a supported speed of the device. Useful.

	DSSS PARAMETER SET:
	Don't use, it changes in packets of the same device. Is the current channel, but for us the channel is chosen manually.

	REQUEST:
	Not so much information online, especially by how many device is used. In our Iphone test does not appear.
	It is a sequence of bytes that specifies which elementID the sender want to receive in the probe response.
	1 byte for each elementID.

	HT CAPABILITIES:
	A lot of byte that defines all the supported specific of devices under 802.11n, so high throughput devices.
	Unfortunatly a byte can change from one packet to another packet of the same device (see iphone_2 - iphone_3)
	But this field is always present, so check its presence but not it's content!

	EXTENDED SUPPORTED RATES:
	Extended Support Rates element specifies the supported rates not carried in the Supported Rates Element.
	It is only required if there are more than 8 supported rates.
	Can be up to 255 bytes!

	SUPPORTED OPERATING CLASSES:
	Not so much information online, especially by how many device is used. In our Iphone test does not appear.
	It defines operating classes supported by the device (channel range, frequency, ecc.).
	It's not so easy to understand.

	20/40 BSS COEXISTANCE:
	Not so much information online, especially by how many device is used. In our Iphone test does not appear.
	Similar to HT capabilities.

	SSID LIST:
	Same device can send packet with different SSID so is not very useful.

	CHANNEL USAGE:
	Not so much information online, especially by how many device is used. In our Iphone test does not appear.
	Details about the band to be used (20/40) and channel.

	INTERWORKING:
	Provides information about the Interworking service capabilities such as the Internet availability
	in a specific service provider network, hotspot, ...
	Unfortunatly can disappear from one packet to another packet of the same device.
	So don't use it.

	MESH ID:
	Not so much information online, especially by how many device is used. In our Iphone test does not appear.
	Needed for 802.11ac for connection to MESH networks

	EXTENDED CAPABILITIES:
	Extend HT_CAPABILITIES, to give other information about device capabilities.
	Unfortunatly can change from one packet to another packet of the same device (iphone_2 - iphone_4, iphone_1 - iphone_8).
	But this field is always present, so check its presence but not it's content!

	VHT CAPABILITIES:
	Not so much information online, especially by how many device is used. In our Iphone test does not appear.
	Needed for device compatible with 802.11ac, so very high throughput.

	VENDOR SPECIFIC:
	Informations about vendor, it contains OUI of the vendor that maybe can be useful.
	There can be more than one, but only the first is constant in packets of a same devices.

*/

// Following fields should not be in probe request packets
#define IE_FH_PARAMETER_SET                     2
#define IE_CF_PARAMETER_SET                     4
#define IE_TIM                                  5
#define IE_IBSS_PARAMETER_SET                   6
#define IE_COUNTRY                              7
#define IE_HOPPING_PATTERN_PARAMETERS           8
#define IE_HOPPING_PATTERN_TABLE                9
#define IE_BSS_LOAD                             11
#define IE_EDCA_PARAMETER_SET                   12
#define IE_TSPEC                                13
#define IE_TCLAS                                14
#define IE_SCHEDULE                             15
#define IE_CHALLENGE_TEXT                       16
#define IE_POWER_CONSTRAINT                     32
#define IE_POWER_CAPABILITY                     33
#define IE_TPC_REQUEST                          34
#define IE_TPC_REPORT                           35
#define IE_SUPPORTED_CHANNELS                   36
#define IE_CHANNEL_SWITCH_ANNOUNCEMENT          37
#define IE_MEASUREMENT_REQUEST                  38
#define IE_MEASUREMENT_REPORT                   39
#define IE_QUIET                                40
#define IE_IBSS_DFS                             41
#define IE_ERP_INFORMATION                      42
#define IE_TS_DELAY                             43
#define IE_TCLAS_PROCESSING                     44
#define IE_QOS_CAPABILITY                       46
#define IE_RSN                                  48
#define IE_AP_CHANNEL_REPORT                    51
#define IE_NEIGHBOR_REPORT                      52
#define IE_RCPI                                 53
#define IE_MOBILITY_DOMAIN                      54
#define IE_FAST_BSS_TRANSITION                  55
#define IE_TIMEOUT_INTERVAL                     56
#define IE_RIC_DATA                             57
#define IE_DSE_REGISTERED_LOCATION              58
#define IE_EXTENDED_CHANNEL_SWITCH_ANNOUNCEMENT 60
#define IE_HT_OPERATION                         61
#define IE_SECONDARY_CHANNEL_OFFSET             62
#define IE_BSS_AVERAGE_ACCESS_DELAY             63
#define IE_ANTENNA                              64
#define IE_RSNI                                 65
#define IE_MEASUREMENT_PILOT_TRANSMISSION       66
#define IE_BSS_AVAILABLE_ADMISSION_CAPACITY     67
#define IE_BSS_AC_ACCESS_DELAY                  68
#define IE_TIME_ADVERTISEMENT                   69
#define IE_RM_ENABLED_CAPACITIES                70
#define IE_MULTIPLE_BSSID                       71
#define IE_20_40_BSS_INTOLERANT_CHANNEL_REPORT  73
#define IE_OVERLAPPING_BSS_SCAN_PARAMETERS      74
#define IE_RIC_DESCRIPTOR                       75
#define IE_MANAGEMENT_MIC                       76
#define IE_EVENT_REQUEST                        78
#define IE_EVENT_REPORT                         79
#define IE_DIAGNOSTIC_REQUEST                   80
#define IE_DIAGNOSTIC_REPORT                    81
#define IE_LOCATION_PARAMETERS                  82
#define IE_NONTRANSMITTED_BSSID_CAPABILITY      83
#define IE_MULTIPLE_BSSID_INDEX                 85
#define IE_FMS_DESCRIPTOR                       86
#define IE_FMS_REQUEST                          87
#define IE_FMS_RESPONSE                         88
#define IE_QOS_TRAFFIC_CAPABILITY               89
#define IE_BSS_MAX_IDLE_PERIOD                  90
#define IE_TFS_REQUEST                          91
#define IE_TFS_RESPONSE                         92
#define IE_WNM_SLEEP_MODE                       93
#define IE_TIM_BROADCAST_REQUEST                94
#define IE_TIM_BROADCAST_RESPONSE               95
#define IE_COLLOCATED_INTERFERENCE_REPORT       96
#define IE_TIME_ZONE                            98
#define IE_DMS_REQUEST                          99
#define IE_DMS_RESPONSE                         100
#define IE_LINK_IDENTIFIER                      101
#define IE_WAKEUP_SCHEDULE                      102
#define IE_CHANNEL_SWITCH_TIMING                104
#define IE_PTI_CONTROL                          105
#define IE_TPU_BUFFER_STATUS                    106
#define IE_ADVERTISEMENT_PROTOCOL               108
#define IE_EXPEDITED_BANDWIDTH_REQUEST          109
#define IE_QOS_MAP_SET                          110
#define IE_ROAMING_CONSORTIUM                   111
#define IE_EMERGENCY_ALART_IDENTIFIER           112
#define IE_MESH_CONFIGURATION                   113
#define IE_MESH_LINK_METRIC_REPORT              115
#define IE_CONGESTION_NOTIFICATION              116
#define IE_MESH_PEERING_MANAGEMENT              117
#define IE_MESH_CHANNEL_SWITCH_PARAMETERS       118
#define IE_MESH_AWAKE_WINDOW                    119
#define IE_BEACON_TIMING                        120
#define IE_MCCAOP_SETUP_REQUEST                 121
#define IE_MCCAOP_SETUP_REPLY                   122
#define IE_MCCAOP_ADVERTISEMENT                 123
#define IE_MCCAOP_TEARDOWN                      124
#define IE_GANN                                 125
#define IE_RANN                                 126
#define IE_PREQ                                 130
#define IE_PREP                                 131
#define IE_PERR                                 132
#define IE_PROXY_UPDATE                         137
#define IE_PROXY_UPDATE_CONFIRMATION            138
#define IE_AUTHENTICATED_MESH_PEERING_EXCHANGE  139
#define IE_MIC                                  140
#define IE_DESTINATION_URI                      141
#define IE_UAPSD_COEXISTENCE                    142
#define IE_MCCAOP_ADVERTISEMENT_OVERVIEW        174
#define IE_VHT_OPERATION                        192
#define IE_EXTENDED_BSS_LOAD                    193
#define IE_WIDE_BANDWIDTH_CHANNEL_SWITCH        194
#define IE_VHT_TRANSMIT_POWER_ENVELOPE          195
#define IE_CHANNEL_SWITCH_WRAPPER               196
#define IE_AID                                  197
#define IE_QUIET_CHANNEL                        198
#define IE_OPERATING_MODE_NOTIFICATION          199
#define IE_EXTENSION                            255
#define IE_EXT_HE_CAPABILITIES                  35
#define IE_EXT_HE_OPERATION                     36