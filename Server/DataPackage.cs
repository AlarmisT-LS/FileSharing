using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class DataPackage
    {
        public long Id { get; } //Id Датаграммы
        public List<byte> Data { get; } //Данные

        public int Count { get; }// Размер данных id + data
        public DataPackage(byte[] arr)
        {
            Data = arr.ToList();
            Count = Data.Count;

            //Перевод первых 8 байтов в id(int64)
            Id = BitConverter.ToInt64(Data.GetRange(0, 8).ToArray());

            //Запись остальной части данных
            Data = Data.GetRange(8, Data.Count - 8);
        }
        public DataPackage(long id, List<byte> data)
        {
            Id = id;
            Data = data;
            Count = Data.Count + BitConverter.GetBytes(Id).Count();
        }

        
        public List<byte> GetPackage()
        {
            List<byte> buffer = new List<byte>();
            //Первые 8 байтов - id Датаграммы
            buffer.AddRange(BitConverter.GetBytes(Id).ToList());
            //Данные
            buffer.AddRange(Data);
            return buffer;
        }


    }
}
