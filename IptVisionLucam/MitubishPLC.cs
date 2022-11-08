using System;
using System.Net;
using System.Net.Sockets;

namespace Ipt
{
    public static class MitubishPLC
    {
        //public static byte[] MakePacketMTypeA1E(int address, bool value)
        //{
        //    int cntWord = 1;
        //    byte[] data = new byte[]
        //    {
        //        0x02, // 0. 서브헤더
        //        0xff, // 1. PLC 번호
        //        0x0a, 0x00, // 2. CPU monitoring timer
        //        0x8a, 0x13, 0x00, 0x00, 0x20, 0x4D, // 4. 선두디바이스
        //        0x01, // 10. 워드 억세스 점수
        //        0x00,
        //        0x01, 0x00 //12
        //    };
        //    data[4] = (byte)(address % 256);
        //    data[5] = (byte)(address / 256);
        //    data[10] = (byte)(cntWord);
        //    data[12] = (byte)(value ? 0x10 : 0x00);
        //    data[13] = 0;
        //    return data;
        //}
        public static byte[] BatchReadBitTypeA1E(string address, int length)
        {
#if _S_MODE_
            if(IptDefines.Active == false)
            {
                return null;
            }
#endif
            int cntWord = length;
            byte[] data = new byte[]
            {
                0x00, // 0. 서브헤더 BatchRead
                0xff, // 1. PLC 번호
                0x0a, 0x00, // 2. CPU monitoring timer
                0x8a, 0x13, 0x00, 0x00, 0x20, 0xff, // 4
                0x01, 0x00// 10. 워드 억세스 점수
                
            };
            MakeAddrDataTypeA1E(4, address, data);
            data[10] = (byte)(cntWord);
            data[11] = (byte)(cntWord >> 8);
            return data;
        }
        public static byte[] BatchReadWordTypeA1E(string address, int length)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            int cntWord = length;
            byte[] data = new byte[]
            {
                0x01, // 0. 서브헤더 BatchRead
                0xff, // 1. PLC 번호
                0x0a, 0x00, // 2. CPU monitoring timer
                0x8a, 0x13, 0x00, 0x00, 0x20, 0xff, // 4
                0x01, 0x00// 10. 워드 억세스 점수
                
            };
            MakeAddrDataTypeA1E(4, address, data);
            data[10] = (byte)(cntWord);
            data[11] = (byte)(cntWord >> 8);
            return data;
        }
        public static byte[] BatchWriteBitTypeA1E(string address, bool value)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            bool[] _value = new bool[1] { value };
            return BatchWriteBitTypeA1E(address, _value);
        }
        public static byte[] BatchWriteBitTypeA1E(string address, bool[] value)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            int cntBits = value.Length;
            byte[] data = new byte[]
            {
                0x02, // 0. 서브헤더
                0xff, // 1. PLC 번호
                0x0a, 0x00, // 2. CPU monitoring timer
                0x8a, 0x13, 0x00, 0x00, 0x20, 0xff, // 4
                0x01, // 10. 억세스 점수
                0x00
            };
            int headerSize = data.Length;
            int dataLength = headerSize + (cntBits + 1) / 2;
            Array.Resize<byte>(ref data, dataLength);
            MakeAddrDataTypeA1E(4, address, data);
            data[10] = (byte)(cntBits);
            for (int i = 0; i < (cntBits + 1) / 2; i++)
            {
                byte v = (byte)(value[i * 2] ? 0x10 : 0x00);
                try
                {
                    v += (byte)(value[i * 2 + 1] ? 0x01 : 0x00);
                }
                catch { }
                data[headerSize + i] = v;
            }
            return data;
        }
        public static byte[] BatchWriteWordTypeA1E(string address, int value)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            int[] _value = new int[1] { value };
            return BatchWriteWordTypeA1E(address, _value);
        }
        public static byte[] BatchWriteWordTypeA1E(string address, int[] value)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            int cntWord = value.Length;
            byte[] data = new byte[]
            {
                0x03, // 0. 서브헤더
                0xff, // 1. PLC 번호
                0x0a, 0x00, // 2. CPU monitoring timer
                0x8a, 0x13, 0x00, 0x00, 0x20, 0xff, // 4
                0x01, 0x00 // 10. 워드 억세스 점수
            };
            int headerSize = data.Length;
            int dataLength = headerSize + (cntWord + 0) * 2;
            Array.Resize<byte>(ref data, dataLength);
            MakeAddrDataTypeA1E(4, address, data);
            data[10] = (byte)(cntWord);
            for (int i = 0; i < cntWord; i++)
            {
                data[headerSize + i * 2] = (byte)(value[i] % 256);
                data[headerSize + i * 2 + 1] = (byte)(value[i] / 256);
            }
            return data;
        }
        public static byte[] RandomWriteBitTypeA1E(string[] address, bool[] value)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            if (address.Length != value.Length) return null;
            int cntBit = value.Length;
            byte[] data = new byte[]
            {
                0x04, // 0. 서브헤더
                0xff, // 1. PLC 번호
                0x0a, 0x00, // 2. CPU monitoring timer
                0x01, 0x00 // 4. 워드 억세스 점수
            };
            int headerSize = data.Length;
            int dataLength = headerSize + (cntBit) * 7;
            Array.Resize<byte>(ref data, dataLength);
            data[4] = (byte)(cntBit);
            data[5] = (byte)(cntBit >> 8);
            int addressHeaderPos = headerSize;
            for (int i = 0; i < cntBit; i++)
            {
                MakeAddrDataTypeA1E(addressHeaderPos, address[i], data);
                addressHeaderPos += 6;
                data[addressHeaderPos++] = (byte)(value[i] ? 1 : 0);
            }
            return data;
        }
        public static byte[] RandomWriteWordTypeA1E(string[] address, int[] value)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            if (address.Length != value.Length) return null;
            int cntWord = value.Length;
            byte[] data = new byte[]
            {
                0x05, // 0. 서브헤더
                0xff, // 1. PLC 번호
                0x0a, 0x00, // 2. CPU monitoring timer
                0x01, 0x00 // 4. 워드 억세스 점수
            };
            int headerSize = data.Length;
            int dataLength = headerSize + (cntWord) * 8;
            Array.Resize<byte>(ref data, dataLength);
            data[4] = (byte)(cntWord);
            data[5] = (byte)(cntWord >> 8);
            int addressHeaderPos = headerSize;
            for (int i = 0; i < cntWord; i++)
            {
                MakeAddrDataTypeA1E(addressHeaderPos, address[i], data);
                addressHeaderPos += 6;
                data[addressHeaderPos++] = (byte)(value[i] % 256);
                data[addressHeaderPos++] = (byte)(value[i] / 256);
            }
            return data;
        }


        public static byte[] WriteStringWord_TypeA1E(string address, string value, int wordLength)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            byte[] data = new byte[]
            {
                0x03, // 0. 서브헤더
                0xff, // 1. PLC 번호
                0x0a, 0x00, // 2. CPU monitoring timer
                0x8a, 0x13, 0x00, 0x00, 0x20, 0xff, // 4
                0x0a, // 10. 워드 억세스 점수
                0x00,
            };
            int headerSize = data.Length;
            int dataLength = headerSize + wordLength * 2;
            Array.Resize<byte>(ref data, dataLength);
            MakeAddrDataTypeA1E(4, address, data);
            data[10] = (byte)(wordLength);
            char[] str = value.ToCharArray();
            try
            {
                for (int i = 0; i < wordLength * 2; i++)
                {
                    data[i + 12] = 0;
                }
                for (int i = 0; i < str.Length; i++)
                {
                    data[i + 12] = (byte)str[i];
                }
            }
            catch { }
            return data;
        }

        public static byte[] BatchReadBitTypeQnA3E(string address, int count)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            byte[] data = new byte[]
            {
                    0x50, 0x00, // 0. subheader Type 3E
                    0x00, // 2. network No.
                    0xff, // 3. PLC No.
                    0xff, 0x03, // 4. destination module io No.
                    0x00, // 6. destination module Station No.
                    0x0c, 0x00, // 7. Request data length
                    0x10, 0x00, // 9. CPU monitoring timer
                    0x01, 0x04, // 11. Command (Batch Read In Word units)
                    0x01, 0x00, // 13. sub command
                    0x00, 0x00,0x00, 0x00, // 15.
                    0x01, 0x00 // 19. 억세스 점수
            };
            // calc. request data length
            data[7] = (byte)((data.Length - 9) % 256);
            data[8] = (byte)((data.Length - 9) / 256);
            // calc. access point
            data[19] = (byte)(count % 256);
            data[20] = (byte)(count / 256);
            MakeAddrDataTypeQnA3E(15, address, data);
            return data;
        }
        public static byte[] BatchReadWordTypeQnA3E(string address, int count)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            byte[] data = new byte[]
            {
                    0x50, 0x00, // 0. subheader Type 3E
                    0x00, // 2. network No.
                    0xff, // 3. PLC No.
                    0xff, 0x03, // 4. destination module io No.
                    0x00, // 6. destination module Station No.
                    0x0c, 0x00, // 7. Request data length
                    0x10, 0x00, // 9. CPU monitoring timer
                    0x01, 0x04, // 11. Command (Batch Read In Word units)
                    0x00, 0x00, // 13. sub command
                    0x00, 0x00,0x00, 0x00, // 15.
                    0x01, 0x00 // 19. 억세스 점수
            };
            // calc. request data length
            data[7] = (byte)((data.Length - 9) % 256);
            data[8] = (byte)((data.Length - 9) / 256);
            // calc. access point
            data[19] = (byte)(count % 256);
            data[20] = (byte)(count / 256);
            MakeAddrDataTypeQnA3E(15, address, data);
            return data;
        }
        public enum PlcErrorCode { OFF_LINE = -1, OK = 0, READ_COUNT_ERRROR, EXCEPTION, BAD_HEADER, SYNTEXT_ERROR }
        //public static short[] BatchReadWordTypeQnA3E(Socket s, string address, int cntWordRead, out PlcErrorCode errFlag, out string strException)
        //{
        //    short[] result = new short[cntWordRead];
        //    errFlag = PlcErrorCode.OFF_LINE;
        //    strException = "";
        //    try
        //    {
        //        if (!s.Connected) return result;
        //        byte[] buffer = new byte[cntWordRead * 2 + 11];
        //        s.Send(BatchReadWordTypeQnA3E(address, cntWordRead));
        //        int cntReceived = s.Receive(buffer);
        //        if (cntReceived <= 0)
        //        {
        //            errFlag = PlcErrorCode.READ_COUNT_ERRROR;
        //            return result;
        //        }
        //        if (buffer[0] != 0xd0)
        //        {
        //            errFlag = PlcErrorCode.BAD_HEADER;
        //            return result;
        //        }
        //        if (cntReceived == 20)
        //        {
        //            errFlag = PlcErrorCode.SYNTEXT_ERROR;
        //            return result;
        //        }
        //        int headerLength = 11;
        //        int dataSize = cntWordRead * 2;
        //        if (cntReceived == (headerLength + dataSize))
        //        {
        //            // TODO 수신된 PLC 접점 데이터 처리
        //            for (int i = 0; i < cntWordRead; i++)
        //            {
        //                int data0 = buffer[headerLength++];
        //                int data1 = buffer[headerLength++];
        //                result[i] = (short)(data0 + (data1 << 8));
        //            }
        //        }

        //        errFlag = PlcErrorCode.OK;
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        strException = ex.Message;
        //        errFlag = PlcErrorCode.EXCEPTION;
        //        return result;
        //    }
        //}
        public static short[] BatchReadWordTypeQnA3E(UdpClient u, IPEndPoint ipep, string address, int cntWordRead, out PlcErrorCode errFlag, out string strException)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                strException = "";
                errFlag = PlcErrorCode.EXCEPTION;
                return null;
            }
#endif
            short[] result = new short[cntWordRead];
            errFlag = PlcErrorCode.OFF_LINE;
            strException = "";
            try
            {
                byte[] data = BatchReadWordTypeQnA3E(address, cntWordRead);
                u.Send(data, data.Length, ipep);
                byte[] buffer = u.Receive(ref ipep);
                if (buffer.Length <= 0)
                {
                    errFlag = PlcErrorCode.READ_COUNT_ERRROR;
                    return result;
                }
                if (buffer[0] != 0xd0)
                {
                    errFlag = PlcErrorCode.BAD_HEADER;
                    return result;
                }
                if (buffer.Length == 20)
                {
                    errFlag = PlcErrorCode.SYNTEXT_ERROR;
                    return result;
                }
                int headerLength = 11;
                int dataSize = cntWordRead * 2;
                if (buffer.Length == (headerLength + dataSize))
                {
                    // TODO 수신된 PLC 접점 데이터 처리
                    for (int i = 0; i < cntWordRead; i++)
                    {
                        int data0 = buffer[headerLength++];
                        int data1 = buffer[headerLength++];
                        result[i] = (short)(data0 + (data1 << 8));
                    }
                }

                errFlag = PlcErrorCode.OK;
                return result;
            }
            catch (Exception ex)
            {
                strException = ex.Message;
                errFlag = PlcErrorCode.EXCEPTION;
                return result;
            }
        }
        public static byte[] BatchWriteBitTypeQnA3E(string address, bool value)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            bool[] _value = new bool[1] { value };
            return BatchWriteBitTypeQnA3E(address, _value);
        }
        public static byte[] BatchWriteBitTypeQnA3E(string address, bool[] value)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            int cntBits = value.Length;
            byte[] data = new byte[]
            {
                    0x50, 0x00, // 0. subheader Type 3E
                    0x00, // 2. network No.
                    0xff, // 3. PLC No.
                    0xff, 0x03, // 4. destination module io No.
                    0x00, // 6. destination module Station No.
                    0x0c, 0x00, // 7. Request data length
                    0x10, 0x00, // 9. CPU monitoring timer
                    0x01, 0x14, // 11. Command (Batch Write In Word units)
                    0x01, 0x00, // 13. sub command
                    0x00, 0x00,0x00, 0x00, // 15.
                    0x01, 0x00 // 19. 억세스 점수
            };
            int headerSize = data.Length;
            int dataLength = headerSize + (cntBits + 1) / 2;
            Array.Resize<byte>(ref data, dataLength);
            // calc. request data length
            data[7] = (byte)((dataLength - 9) % 256);
            data[8] = (byte)((dataLength - 9) / 256);
            // calc. access point
            data[19] = (byte)(cntBits);
            data[20] = (byte)(cntBits >> 8);
            MakeAddrDataTypeQnA3E(15, address, data);
            for (int i = 0; i < (cntBits + 1) / 2; i++)
            {
                byte v = (byte)(value[i * 2] ? 0x10 : 0x00);
                try
                {
                    v += (byte)(value[i * 2 + 1] ? 0x01 : 0x00);
                }
                catch { }
                data[headerSize + i] = v;
            }
            return data;
        }
        //public static PlcErrorCode BatchWriteWordTypeQnA3E(Socket s, string address, short value, out string strException)
        //{
        //    short[] _value = new short[1] { value };
        //    return BatchWriteWordTypeQnA3E(s, address, _value, out strException);
        //}
        //public static PlcErrorCode BatchWriteWordTypeQnA3E(Socket s, string address, short[] value, out string strException)
        //{
        //    strException = "";
        //    try
        //    {
        //        byte[] buffer = new byte[1024];
        //        s.Send(BatchWriteWordTypeQnA3E(address, value));
        //        int cnt = s.Receive(buffer);
        //        if (cnt <= 0)
        //        {
        //            return PlcErrorCode.READ_COUNT_ERRROR;
        //        }
        //        return PlcErrorCode.OK;
        //    }
        //    catch(Exception ex)
        //    {
        //        strException = ex.Message;
        //        return PlcErrorCode.EXCEPTION;
        //    }
        //}
        public static PlcErrorCode BatchWriteWordTypeQnA3E(UdpClient u, IPEndPoint ipep, string address, short value, out string strException)
        {
            short[] _value = new short[1] { value };
            return BatchWriteWordTypeQnA3E(u, ipep, address, _value, out strException);
        }
        public static PlcErrorCode BatchWriteWordTypeQnA3E(UdpClient u, IPEndPoint ipep, string address, short[] value, out string strException)
        {
            strException = "";
            try
            {
                byte[] data = BatchWriteWordTypeQnA3E(address, value);
                u.Send(data, data.Length, ipep);
                byte[] buffer = u.Receive(ref ipep);
                if (buffer.Length <= 0)
                {
                    return PlcErrorCode.READ_COUNT_ERRROR;
                }
                return PlcErrorCode.OK;
            }
            catch (Exception ex)
            {
                strException = ex.Message;
                return PlcErrorCode.EXCEPTION;
            }
        }

        public static byte[] BatchWriteWordTypeQnA3E(string address, int value)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            int[] _value = new int[1] { value };
            return BatchWriteWordTypeQnA3E(address, _value);
        }
        public static byte[] BatchWriteWordTypeQnA3E(string address, int[] value)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            int cntWord = value.Length;
            byte[] data = new byte[]
            {
                    0x50, 0x00, // 0. subheader Type 3E
                    0x00, // 2. network No.
                    0xff, // 3. PLC No.
                    0xff, 0x03, // 4. destination module io No.
                    0x00, // 6. destination module Station No.
                    0x0c, 0x00, // 7. Request data length
                    0x10, 0x00, // 9. CPU monitoring timer
                    0x01, 0x14, // 11. Command (Batch Write In Word units)
                    0x00, 0x00, // 13. sub command
                    0x00, 0x00,0x00, 0x00, // 15.
                    0x01, 0x00 // 19. 워드 억세스 점수
            };
            int headerSize = data.Length;
            int dataLength = headerSize + (cntWord + 0) * 2;
            Array.Resize<byte>(ref data, dataLength);
            // calc. request data length
            data[7] = (byte)((dataLength - 9) % 256);
            data[8] = (byte)((dataLength - 9) / 256);
            // calc. access point
            data[19] = (byte)(cntWord);
            data[20] = (byte)(cntWord >> 8);
            MakeAddrDataTypeQnA3E(15, address, data);
            for (int i = 0; i < cntWord; i++)
            {
                data[headerSize + i * 2] = (byte)(value[i] % 256);
                data[headerSize + i * 2 + 1] = (byte)(value[i] / 256);
            }
            return data;
        }
        public static byte[] BatchWriteWordTypeQnA3E(string address, short[] value)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            int cntWord = value.Length;
            byte[] data = new byte[]
            {
                    0x50, 0x00, // 0. subheader Type 3E
                    0x00, // 2. network No.
                    0xff, // 3. PLC No.
                    0xff, 0x03, // 4. destination module io No.
                    0x00, // 6. destination module Station No.
                    0x0c, 0x00, // 7. Request data length
                    0x10, 0x00, // 9. CPU monitoring timer
                    0x01, 0x14, // 11. Command (Batch Write In Word units)
                    0x00, 0x00, // 13. sub command
                    0x00, 0x00,0x00, 0x00, // 15.
                    0x01, 0x00 // 19. 워드 억세스 점수
            };
            int headerSize = data.Length;
            int dataLength = headerSize + (cntWord + 0) * 2;
            Array.Resize<byte>(ref data, dataLength);
            // calc. request data length
            data[7] = (byte)((dataLength - 9) % 256);
            data[8] = (byte)((dataLength - 9) / 256);
            // calc. access point
            data[19] = (byte)(cntWord);
            data[20] = (byte)(cntWord >> 8);
            MakeAddrDataTypeQnA3E(15, address, data);
            for (int i = 0; i < cntWord; i++)
            {
                data[headerSize + i * 2] = (byte)(value[i] % 256);
                data[headerSize + i * 2 + 1] = (byte)(value[i] / 256);
            }
            return data;
        }
        public static byte[] WriteStringWord_TypeQnA3E(string address, string value, int wordLength)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            byte[] data = new byte[]
            {
                    0x50, 0x00, // 0. subheader Type 3E
                    0x00, // 2. network No.
                    0xff, // 3. PLC No.
                    0xff, 0x03, // 4. destination module io No.
                    0x00, // 6. destination module Station No.
                    0x0c, 0x00, // 7. Request data length
                    0x10, 0x00, // 9. CPU monitoring timer
                    0x01, 0x14, // 11. Command (Batch Write In Word units)
                    0x00, 0x00, // 13. sub command
                    0x00, 0x00,0x00, 0x00, // 15.
                    0x01, 0x00 // 19. 워드 억세스 점수
            };
            int headerSize = data.Length;
            int dataLength = headerSize + wordLength * 2;
            Array.Resize<byte>(ref data, dataLength);
            // calc. request data length
            data[7] = (byte)((dataLength - 9) % 256);
            data[8] = (byte)((dataLength - 9) / 256);
            // calc. access point
            data[19] = (byte)(wordLength);
            data[20] = (byte)(wordLength >> 8);
            MakeAddrDataTypeQnA3E(15, address, data);
            char[] str = value.ToCharArray();
            try
            {
                for (int i = 0; i < wordLength * 2; i++)
                {
                    data[i + 21] = 0;
                }
                for (int i = 0; i < str.Length; i++)
                {
                    data[i + 21] = (byte)str[i];
                }
            }
            catch { }
            return data;
        }

        public static byte[] RandomReadWordTypeQnA3E(string address)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            string[] _address = new string[1] { address };
            return RandomReadWordTypeQnA3E(_address);
        }
        public static byte[] RandomReadWordTypeQnA3E(string[] address)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            byte[] data = new byte[]
            {
                    0x50, 0x00, // subheader Type 3E
                    0x00, // network No.
                    0xff, // PLC No.
                    0xff, 0x03, // destination module io No.
                    0x00, // destination module Station No.
                    0x0c, 0x00, // 7. Request data length
                    0x10, 0x00, // CPU monitoring timer
                    0x03, 0x04, // Command (Random Read In Word units)
                    0x00, 0x00, // sub command
                    0x01, // 15. 워드 억세스 점수
                    0x00 // 16. 더블 워드 억세스 점수
            };
            int headerSize = data.Length;
            int dataLength = headerSize + (address.Length + 0) * 4;
            Array.Resize<byte>(ref data, dataLength);
            // calc. request data length
            data[7] = (byte)((data.Length - 9) % 256);
            data[8] = (byte)((data.Length - 9) / 256);
            // calc. access point
            data[15] = (byte)address.Length;
            data[16] = (byte)0;
            int addressHeaderPos = 17;
            foreach (string addr in address)
            {
                MakeAddrDataTypeQnA3E(addressHeaderPos, addr, data);
                addressHeaderPos += 4;
            }
            return data;
        }
        public static byte[] RandomWriteBitTypQnA3E(string[] address, bool[] value)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            if (address.Length != value.Length) return null;
            int cntBit = value.Length;
            byte[] data = new byte[]
            {
                    0x50, 0x00, // subheader Type 3E
                    0x00, // network No.
                    0xff, // PLC No.
                    0xff, 0x03, // destination module io No.
                    0x00, // destination module Station No.
                    0x0c, 0x00, // 7. Request data length
                    0x10, 0x00, // CPU monitoring timer
                    0x02, 0x14, // Command (Random Write In Word units)
                    0x01, 0x00, // sub command
                    0x01 // 15. 비트 억세스 점수
            };
            int headerSize = data.Length;
            int dataLength = headerSize + (cntBit) * 5;
            Array.Resize<byte>(ref data, dataLength);
            data[15] = (byte)(cntBit);
            int addressHeaderPos = headerSize;
            for (int i = 0; i < cntBit; i++)
            {
                MakeAddrDataTypeQnA3E(addressHeaderPos, address[i], data);
                addressHeaderPos += 4;
                data[addressHeaderPos++] = (byte)(value[i] ? 1 : 0);
            }
            return data;
        }
        public static byte[] RandomWriteWordTypQnA3E(string[] address, int[] value)
        {
#if _S_MODE_
            if (IptDefines.Active == false)
            {
                return null;
            }
#endif
            if (address.Length != value.Length) return null;
            int cntWord = value.Length;
            byte[] data = new byte[]
            {
                    0x50, 0x00, // subheader Type 3E
                    0x00, // network No.
                    0xff, // PLC No.
                    0xff, 0x03, // destination module io No.
                    0x00, // destination module Station No.
                    0x0c, 0x00, // 7. Request data length
                    0x10, 0x00, // CPU monitoring timer
                    0x02, 0x14, // Command (Random Write In Word units)
                    0x00, 0x00, // sub command
                    0x01, // 15. 워드 억세스 점수
                    0x00 // 16. 더블 워드 억세스 점수
            };
            int headerSize = data.Length;
            int dataLength = headerSize + (cntWord) * 6;
            Array.Resize<byte>(ref data, dataLength);
            data[15] = (byte)(cntWord);
            data[16] = 0;
            int addressHeaderPos = headerSize;
            for (int i = 0; i < cntWord; i++)
            {
                MakeAddrDataTypeQnA3E(addressHeaderPos, address[i], data);
                addressHeaderPos += 4;
                data[addressHeaderPos++] = (byte)(value[i] % 256);
                data[addressHeaderPos++] = (byte)(value[i] / 256);
            }
            return data;
        }


        private static void MakeAddrDataTypeA1E(int addressHeaderPos, String addr, byte[] data)
        {
            string name = addr;
            string type = name.Substring(0, 1).ToUpper();
            string value = name.Substring(1).ToUpper();
            int address = 0;
            switch (type)
            {
                case "X":
                case "Y":
                case "B":
                case "W":
                    address = Convert.ToInt32(value, 16);
                    break;
                default:
                    address = Convert.ToInt32(value, 10);
                    break;
            }
            data[addressHeaderPos++] = (byte)(address & 0x000000ff);
            data[addressHeaderPos++] = (byte)((address >> 8) & 0x000000ff);
            data[addressHeaderPos++] = (byte)((address >> 16) & 0x000000ff);
            data[addressHeaderPos++] = (byte)((address >> 24) & 0x000000ff);
            switch (type)
            {
                case "D":
                    data[addressHeaderPos++] = 0x20;
                    data[addressHeaderPos++] = 0x44;
                    break;
                case "R":
                    data[addressHeaderPos++] = 0x20;
                    data[addressHeaderPos++] = 0x52;
                    break;
                case "X":
                    data[addressHeaderPos++] = 0x20;
                    data[addressHeaderPos++] = 0x58;
                    break;
                case "Y":
                    data[addressHeaderPos++] = 0x20;
                    data[addressHeaderPos++] = 0x59;
                    break;
                case "M":
                    data[addressHeaderPos++] = 0x20;
                    data[addressHeaderPos++] = 0x4d;
                    break;
            }
        }
        private static void MakeAddrDataTypeQnA3E(int addressHeaderPos, string addr, byte[] data)
        {
            string name = addr;
            string type = name.Substring(0, 1).ToUpper();
            string value = name.Substring(1).ToUpper();
            int address;
            switch (type)
            {
                case "X":
                case "Y":
                case "B":
                case "W":
                    address = Convert.ToInt32(value, 16);
                    break;
                default:
                    address = Convert.ToInt32(value, 10);
                    break;
            }
            data[addressHeaderPos++] = (byte)(address & 0x000000ff);
            data[addressHeaderPos++] = (byte)((address >> 8) & 0x000000ff);
            data[addressHeaderPos++] = (byte)((address >> 16) & 0x000000ff);

            switch (type)
            {
                case "X":
                    data[addressHeaderPos++] = 0x9c;
                    break;
                case "Y":
                    data[addressHeaderPos++] = 0x9d;
                    break;
                case "M":
                    data[addressHeaderPos++] = 0x90;
                    break;
                case "L":
                    data[addressHeaderPos++] = 0x92;
                    break;
                case "B":
                    data[addressHeaderPos++] = 0xa0;
                    break;
                case "D":
                    data[addressHeaderPos++] = 0xa8;
                    break;
                case "R":
                    data[addressHeaderPos++] = 0xaf;
                    break;
                case "W":
                    data[addressHeaderPos++] = 0xb4;
                    break;
            }
        }
    }
}
