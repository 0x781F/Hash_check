using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace HashCheck
{
    public partial class Form1 : Form
    {
        long offset = 0x00000000; // 0 bytes
        long length = 0x00000100; // 256 bytes
        //long minB = 0;
        //long maxB = 0;
        
        SHA1 sha = new SHA1CryptoServiceProvider();
        string PATH;
        string MYHASH;
        byte[] MYHASHar;
        long FILELENGTH;
        long LASTRECORD;
        MemoryMappedFile mmf;// = MemoryMappedFile.CreateFromFile(PATH, FileMode.Open, "hashes");
        MemoryMappedViewAccessor viewAccessor;

        bool FileIsLoaded = false;

        void CloseBigFile()
        {
            if (FileIsLoaded)
            {
                this.Text = "781F PWN CHECK";
                FileIsLoaded = false;
                viewAccessor.Dispose();
                mmf.Dispose();
                GC.Collect();
            }
        }
        Timer BlinkTimer;
        void blinkBOOL(bool isGood)
        {
            if (isGood)
            {
                textBox2.BackColor = Color.Lime;
            }
            else
            {
                textBox2.BackColor = Color.Red;
            }
            BlinkTimer.Start();
        }

        private void BlinkTimer_Tick(object sender, EventArgs e)
        {
            textBox2.BackColor = SystemColors.Control;
            BlinkTimer.Stop();
        }

        bool OpenDialog()
        {
            bool FileIsInFolder=false;
            switch (ChosenModeID)
            {
                case 2:
                case 3:
                    if (File.Exists(@".\pwned-passwords-ordered-by-hash.txt"))
                    {
                        PATH = @".\pwned-passwords-ordered-by-hash.txt";
                        FileIsInFolder = true;
                    }
                    break;
                case 4:
                    if (File.Exists(@".\pwned-passwords-ntlm-ordered-by-hash.txt"))
                    {
                        PATH = @".\pwned-passwords-ntlm-ordered-by-hash.txt";
                        FileIsInFolder = true;
                    }
                        break;
                default:
                    throw new Exception("Wrong mode ID");

            }
            if (FileIsInFolder)
            {
                return LoadNewHashFile(PATH);
            }
            MessageBox.Show("Please open sorted hashes list", "", MessageBoxButtons.OK);
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return LoadNewHashFile(openFileDialog1.FileName);
            }
            else
            {
                return false;
            }
        }
        MemoryMappedFileSecurity mmfS = new MemoryMappedFileSecurity();
        bool LoadNewHashFile(string PATH)
        {
            try
            {
                mmf = MemoryMappedFile.CreateFromFile(File.Open(PATH, FileMode.Open, FileAccess.Read, FileShare.Read), "hashes",0, MemoryMappedFileAccess.Read, mmfS, HandleInheritability.None,false);
                FILELENGTH = new System.IO.FileInfo(PATH).Length;
                LASTRECORD = findlast();
                viewAccessor = mmf.CreateViewAccessor(offset, length,MemoryMappedFileAccess.Read);
                this.Text = "781F PWN CHECK - " + PATH;
                FileIsLoaded = true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error when opening file");
                return false;
            }
            return true;
        }

        public Form1()
        {
            BlinkTimer = new Timer();
            BlinkTimer.Interval = 1337;
            BlinkTimer.Tick += BlinkTimer_Tick;

            InitializeComponent();


        }

        private long findlast()
        {
            byte[] bytes = new byte[256];
            viewAccessor = mmf.CreateViewAccessor(FILELENGTH-256, 256, MemoryMappedFileAccess.Read);
            viewAccessor.ReadArray(0, bytes, 0, bytes.Length);
            ;
            for (int i = 254; i != 0; i--)
            {
                if (bytes[i] == 10)
                {
                    return (i + FILELENGTH - 256);
                }
            }
            throw new Exception("Can't find last record. Wrong format?");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!FileIsLoaded)
            {
                if (!OpenDialog())
                {
                    return;
                }
            }
            switch (ChosenModeID)
            {
                case 2:
                    if (textBox1.Text.Length > 0)
                    {
                        this.Enabled = false;
                        if (IsListFromFileLoaded)
                        {
                            foreach(string Pass in InputList)
                            {
                                CheckOnePlaintextPassword(Pass);
                            }
                        }
                        else
                        {
                            CheckOnePlaintextPassword(textBox1.Text);
                        }
                        this.Enabled = true;
                    }
                    break;
                case 3:
                    this.Enabled = false;
                    if (IsListFromFileLoaded)
                    {
                        foreach (string Hash in InputList)
                        {
                            CheckOneSHA1HASH(Hash);
                        }
                    }
                    else
                    {
                        CheckOneSHA1HASH(textBox1.Text);
                    }
                    this.Enabled = true;
                    break;
                case 4:
                    this.Enabled = false;
                    if (IsListFromFileLoaded)
                    {
                        foreach (string Hash in InputList)
                        {
                            CheckOneNTLMHASH(Hash);
                        }
                    }
                    else
                    {
                        CheckOneNTLMHASH(textBox1.Text);
                    }
                    this.Enabled = true;
                    break;
                default:
                    throw new Exception("Unknown mode set");

            }



            //char[] CA = new char[40];


        }
        private bool IsHex(IEnumerable<char> chars)
        {
            bool isHex;
            foreach (var c in chars)
            {
                isHex = ((c >= '0' && c <= '9') ||
                         (c >= 'a' && c <= 'f') ||
                         (c >= 'A' && c <= 'F'));

                if (!isHex)
                    return false;
            }
            return true;
        }

        private void CheckOneSHA1HASH(string hashline)
        {
            if (!IsHex(hashline) || hashline.Length!=40)
            {
                textBox2.AppendText(Environment.NewLine + "[" + hashline + "]" + Environment.NewLine + "!ERROR! SHA-1 hash must contain 40 HEX digits, skipping...");
                return;
            }
            MYHASH=hashline.ToUpper();
            
            MYHASHar = Encoding.ASCII.GetBytes(MYHASH);
            textBox2.AppendText(Environment.NewLine + "[" + hashline + "]");

            process(MYHASHar,40);

        }
        private void CheckOneNTLMHASH(string hashline)
        {
            if (!IsHex(hashline) || hashline.Length != 32)
            {
                textBox2.AppendText(Environment.NewLine + "[" + hashline + "]" + Environment.NewLine + "!ERROR! NTLM hash must contain 32 HEX digits, skipping...");
                return;
            }
            MYHASH = hashline.ToUpper();

            MYHASHar = Encoding.ASCII.GetBytes(MYHASH);
            textBox2.AppendText(Environment.NewLine + "[" + hashline + "]");

            process(MYHASHar, 32);

        }

        private void CheckOnePlaintextPassword(string passline)
        {
            byte[] byteArray_out = sha.ComputeHash(Encoding.ASCII.GetBytes(passline));

            MYHASH = BitConverter.ToString(byteArray_out).Replace("-", string.Empty); //new string(CA); //Encoding.ASCII.GetString(byteArray_out);
            MYHASHar = Encoding.ASCII.GetBytes(MYHASH);
            textBox2.AppendText(Environment.NewLine + "[" + passline + "]" + Environment.NewLine + "Your password hash is: " + Environment.NewLine + MYHASH + Environment.NewLine + "Searching...");
            
            process(MYHASHar,40);

        }




        long start = 0;
        long end;//= LASTRECORD;

        void process(byte[] MYHASHar, int hashLEN)
        {
            start = 0;
            end = LASTRECORD;
            bool done = false;
            //long recbinsearch_ANSWER;
            long LastStart=-1;
            long LastEnd = -1;
            int repeats = 0;

            while (!done)
            {
                switch (recbinsearch(start, end, MYHASHar, hashLEN))
                {
                    case 1:
                        end = (start + end) / 2;
                        break;
                    case -1:
                        start = ((start + end) / 2);
                        break;
                    case 0:
                        done = true;
                        break;
                    case -2:
                        start = RIGHTPOS;
                        end = RIGHTPOS;
                        done = true;
                        break;
                    default:
                        throw new Exception("WTFlol");
                }
                if (LastStart == start && LastEnd == end )
                {
                    //repeats++;
                    done = true;
                    textBox2.AppendText(Environment.NewLine+"NOT FOUND"+ Environment.NewLine+"closest is ");
                    string textS = Encoding.ASCII.GetString(GetFirstHashAt(start, hashLEN));
                    string textE = Encoding.ASCII.GetString(GetFirstHashAt(end, hashLEN));
                    textBox2.AppendText(Environment.NewLine + textS + Environment.NewLine+" position:" + start.ToString());
                    textBox2.AppendText(Environment.NewLine + textE + Environment.NewLine+" position:" + end.ToString());
                    blinkBOOL(true);
                    return;
                }
                LastStart = start;
                LastEnd = end;
            }
            var a = GetFirstHashAt(start, hashLEN);
            if (a == null)
            {
                textBox2.AppendText(Environment.NewLine+"[ Data Format ERROR at line " + start.ToString() +" ]");
                return;
            }
            string text = Encoding.ASCII.GetString(a);
            textBox2.AppendText(Environment.NewLine + "YOUR PASS WAS PWND!!! " + Environment.NewLine + text+ Environment.NewLine+" position:"+ start.ToString());
            blinkBOOL(false);

        }

        string lastfirst;
        string lastlast;
        long RIGHTPOS = -1;

        int recbinsearch(long start,long end, byte[] ST, int hashLEN)
        {
            byte[] first= GetFirstHashAt(start, hashLEN);
            byte[] last= GetFirstHashAt(end, hashLEN);
            byte[] mid;
            lastfirst = Encoding.ASCII.GetString(first)+"/"+ start.ToString();
            lastlast = Encoding.ASCII.GetString(last) + "/" + end.ToString();
            if (!first.SequenceEqual(last))
            {
                mid = GetFirstHashAt((start + end) / 2, hashLEN);
                if (mid.SequenceEqual(ST))
                {
                    RIGHTPOS = (start + end) / 2;
                    return -2;
                    //
                }
                if (IsMidGreaterThanSEARCHTERM(mid, ST))
                {
                    return 1;//(recbinsearch(start, (start + end) / 2, ST));
                }
                else
                {
                    return -1;// (recbinsearch((start + end), end / 2, ST));
                }
            }
            else
            {
                return 0;//(end);
            }
        }
        bool IsMidGreaterThanSEARCHTERM(byte[] mid, byte[] ST)
        {
            for(int i = 0; i < mid.Length; i++)
            {
                if (mid[i] > ST[i])
                {
                    return true;
                }
                if (mid[i] < ST[i])
                {
                    return false;
                }
            }
            throw new Exception("its fucked");
        }

        byte[] GetFirstHashAt(long l_index, int hashLEN)
        {
            long offset = 0x00000000; 
            long length = 0x00000100;
            if (l_index > LASTRECORD) { return null; }
            if (l_index > offset + length || l_index < offset)
            {
                if (l_index + length > FILELENGTH)
                {
                    length = FILELENGTH - l_index;
                }
                offset = l_index ;
            }
            viewAccessor = mmf.CreateViewAccessor(offset, length, MemoryMappedFileAccess.Read);
            long index = l_index - offset;
            byte delta = 0;
            bool done = false;
            int i = 0;
            byte[] bytes = new byte[256 - delta];
            byte[] answer = new byte[hashLEN];

            ;
            if (index + 256 > FILELENGTH)
            {
                delta = (byte)(FILELENGTH - index);
                index = FILELENGTH - 256;
            }
            ;
            viewAccessor.ReadArray(index+delta, bytes, 0, bytes.Length);
            while (!done)
            {
                if((bytes[i]==0x3A) && i>= hashLEN)
                {
                    for (int j = 0; j< hashLEN; j++)
                    {
                        answer[j] = bytes[i - hashLEN + j];
                    }
                    
                    return answer;
                }
                i ++;
                if (i > 150)
                {
                    done = true;
                }
            }
            throw new Exception("[ Data Format ERROR at line " + l_index.ToString() + " ]");
        }

        byte ChosenModeID=2;
        private void button2_Click(object sender, EventArgs e) //passes
        {
            if (ChosenModeID == 4) CloseBigFile();
            button2.Enabled = false;
            button3.Enabled = true;
            button4.Enabled = true;
            ChosenModeID = 2;
            UnloadList();
        }

        private void button3_Click(object sender, EventArgs e) //hashes
        {
            if(ChosenModeID==4) CloseBigFile();
            button2.Enabled = true;
            button3.Enabled = false;
            button4.Enabled = true;
            ChosenModeID = 3;
            UnloadList();
        }

        private void button4_Click(object sender, EventArgs e) //ntlm
        {
            if (ChosenModeID == 2 || ChosenModeID == 3) CloseBigFile();
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = false;
            ChosenModeID = 4;
            UnloadList();
        }

        private void Save_Log_button_Click(object sender, EventArgs e)
        {
            
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.DefaultExt = "txt";
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog1.FileName, textBox2.Text);
            }

        }

        bool IsListFromFileLoaded=false;
        List<string> InputList;

        private void Load_button_Click(object sender, EventArgs e)
        {
            if (IsListFromFileLoaded)
            {
                UnloadList();
                return;
            }

            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                InputList = new List<string>(File.ReadAllLines(openFileDialog1.FileName));
                IsListFromFileLoaded = true;
                textBox1.Text = openFileDialog1.FileName;
                textBox1.Enabled = false;
                Load_button.Text = "Unload list";
            }
        }
        void UnloadList()
        {
            Load_button.Text = "Load list from file";
            IsListFromFileLoaded = false;
            InputList = null;
            textBox1.Enabled = true;
            textBox1.Text = "";
        }

        private void ClearLogButton_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("Clear Log?", "Confirm", MessageBoxButtons.OKCancel)==DialogResult.OK)
                textBox2.Text = "";
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
