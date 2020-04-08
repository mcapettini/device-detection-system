Firmware developed for ESP-IDF version 3.2

Before compilation:
1) go to \esp\esp-idf\components\lwip\lwip\src\apps
2) rename the original sntp.c file in sntp.c.bak
3) copy the sntp.c file you find in the root folder of this project to \esp\esp-idf\components\lwip\lwip\src\apps

Requirements for sdkconfig:
C++ exceptions enabled
set COM port to flash firmware
TCP_SND_BUF_DEFAULT 12000 (default 5744)