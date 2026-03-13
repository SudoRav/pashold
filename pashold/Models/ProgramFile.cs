using System.Collections.ObjectModel;

namespace pashold.Models
{
    public class ProgramFile
    {
        public string OriginalName { get; set; }  // не зашифровано
        public string EncryptedName { get; set; } // можно оставить, шифрование не обязательно
        public string FilePath { get; set; }

        public ObservableCollection<Block> Blocks { get; set; } = new ObservableCollection<Block>();
    }
}