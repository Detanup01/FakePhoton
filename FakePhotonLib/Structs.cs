namespace FakePhotonLib;

/*
00 00 // start number (skipped)
00 // is encrypted or CRC
02 // command count

5B DB 01 2B // server time 
02 EA 8F 04 // challenge

// 1. command
01 // command type
FF // channel id
00 // command flag
00 // reserved byte
00 00 00 14 // size
00 00 00 00 // reliableSequenceNumber
00 00 00 01 // ackReceivedReliableSequenceNumber
00 00 00 56 // ackReceivedSentTime

// no payload?


03 
FF 
01 
00 
00 00 00 2C 
00 00 00 01 
46 C9 04 B0  // peer Id?
// unknown
00 00 80 00 00 00 00 02 00 00 00 00 00 00 00 00 00 00 13 88 00 00 00 02 00 00 00 02

*/


/*
46 C9 
00 
02 
00 00 00 8D
02 EA 8F 04 

01
FF 
00 
04 
00 00 00 14 
00 00 00 00 
00 00 00 01
5B DB 01 2B 

06 
00 
01 
04 
00 00 00 35
00 00 00 01 

F3 // operation message
00 // is encrypted?
01 08 // idk why we skip these

1E 41 08 00 00 4E 61 6D 65 53 65 72 76 65 72 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00

 */