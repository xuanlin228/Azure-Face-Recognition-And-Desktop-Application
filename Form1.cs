using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web.Script.Serialization;

namespace facewin
{
    
    public partial class Form1 : Form
    {
        string faceApiKey;
        string faceApiUrl = "faceApiurl";
        string imagePath;
        FaceServiceClient faceServiceClient;
        Face[] objFace;
        string _groupId;
        string list_result;
        string faceid;
        List<Person> Persons = new List<Person>();
        public Form1(/*string _faceApiKey*/)
        {

            //faceApiKey = _faceApiKey;
            InitializeComponent();
            faceApiKey = "faceapiKey";
            button2.Visible = false;
            _groupId = "face03";
            
        }
        private async Task<string> GetFaceId(string _imagePath, string _faceApiUrl, string _faceApiKey)
        {
            string str = "";
            faceServiceClient = new FaceServiceClient(_faceApiKey, _faceApiUrl);
            
            FaceAttributeType[] fa = new FaceAttributeType[]
            {
                FaceAttributeType.Age,
                FaceAttributeType.Gender,
                FaceAttributeType.Emotion,
            };
            
            objFace = await faceServiceClient.DetectAsync(File.OpenRead(imagePath), true, false, fa);
            if (objFace.Length > 0)
                for (int i = 0; i < objFace.Length; i++)
                    str += "\"" + objFace[i].FaceId.ToString()+"\"" + ",";
            else
                str = "無法辨識，重新選擇";
            return str;
        }
        private async void button1_Click(object sender, EventArgs e)
        {
            DialogResult dr = openFileDialog1.ShowDialog();
            string filename = openFileDialog1.FileName;
            textBox2.Text = "";
            richTextBox1.Text = "";
            if (dr == System.Windows.Forms.DialogResult.OK && !string.IsNullOrEmpty(filename))
            {
                imagePath = openFileDialog1.FileName;

                panel1.BackgroundImage = new Bitmap(imagePath);
                panel1.BackgroundImageLayout = ImageLayout.Zoom;
                panel1.Refresh();
                textBox1.Text = imagePath;
                faceid = await GetFaceId(imagePath, faceApiUrl, faceApiKey);
                button2.Visible = true;
            }
            
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            if (objFace.Length > 0)
            {
                
                for (int i = 0; i < objFace.Length; i++)
                {
                    FaceRectangle fRectangle = objFace[i].FaceRectangle;
                    double BImageWidth = panel1.BackgroundImage.Width;
                    double BImageHeight = panel1.BackgroundImage.Height;

                    //care of coordinate change
                    double xx = 0, yy = 0;
                    double scale = panel1.Width / BImageWidth;
                    if (scale >= panel1.Height / BImageHeight)
                    {
                        scale = panel1.Height / BImageHeight;
                        xx = (panel1.Width - scale * BImageWidth) / 2.0;
                    }
                    else
                    {
                        yy = (panel1.Height - scale * BImageHeight) / 2.0;
                    }
                    Graphics g = panel1.CreateGraphics();
                    Pen[] p = new Pen[2];
                    p[0] = new Pen(Color.Yellow, 3);
                    p[1] = new Pen(Color.Blue, 3);
                    int left, top, width, height;

                    left = (int)(panel1.Width * fRectangle.Left);
                    top = (int)(panel1.Height * fRectangle.Top);
                    width = (int)(panel1.Width * fRectangle.Height);
                    height = (int)(panel1.Height * fRectangle.Width);
                    g.DrawRectangle(p[i % 2], left, top, width, height);

                    


                    string strinfo = $"性別:{objFace[i].FaceAttributes.Gender }\r\n" +
                    $"年齡: {objFace[i].FaceAttributes.Age}\r\n============\r\n";
                    identify(strinfo);
                    //textBox2.Text += strinfo;
                    var PersonGroup =await list();
                    for(int k=0;k<PersonGroup.Count;k++)
                    {
                        richTextBox1.SelectionColor = Color.BlueViolet;
                        richTextBox1.Text +="Url: "+ PersonGroup[k].Userdata+"\r\n";
                        richTextBox1.SelectionColor = Color.RosyBrown;
                        richTextBox1.Text += "Name: " + PersonGroup[k].Name + "\r\n";
                        richTextBox1.SelectionColor = Color.Black;
                        richTextBox1.Text += "\r\n===========\r\n";
                    }
                }
            }
            else
            {
                MessageBox.Show("Please Choose a photo with face\r\n");
            }
        }
        private async void identify(string info)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "3e652220978f4479b3258dd90c71d8ce");

            var uri = "https://eastasia.api.cognitive.microsoft.com/face/v1.0/identify?" + queryString;

            HttpResponseMessage response;
            //string id= await GetFaceId(imagePath, faceApiUrl, faceApiKey); ;
            string body = "{\"PersonGroupId\": \"face03\",\"faceIds\": [" + faceid + "],\"maxNumOfCandidatesReturned\": 1,\"confidenceThreshold\": 0.5}";
            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes(body);
            string result;
            JArray jArray;

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
                result = await response.Content.ReadAsStringAsync();
                jArray = JArray.Parse(result);
            }
            string faceId;
            JToken candidates;
            foreach (JObject json in jArray)
            {
                int i = 0;
                faceId = json.GetValue("faceId").ToString();
                candidates = json.GetValue("candidates");
                string name = "";
                foreach (JObject j in candidates)
                {
                    string personId;
                    string confidence;
                    i++;
                    personId = j.GetValue("personId").ToString();
                    confidence = j.GetValue("confidence").ToString();
                    //textBox2.Text += "personid: " + confidence + "\r\n";
                    textBox2.Text += "confidence: " + confidence + "\r\n";

                    var PersonGroup = await list();
                    Console.WriteLine(PersonGroup.Count);
                    for (int k = 0; k < PersonGroup.Count; k++)
                    {
                        if (personId == PersonGroup[k].Personid)
                        {
                            name = Persons[k].Name;
                        }
                    }
                    //textBox2.Text += "Candidates: "+ candidates + "\r\n";
                }
                if (i == 0)
                {
                    textBox2.Text += "confidence:  Sorry~~ Not match person\r\n";
                    textBox2.Text += "faceId: " + faceId + "\r\n";
                    textBox2.Text += "\r\n======\r\n";
                }
                else
                {
                    textBox2.Text += "faceId: " + faceId + "\r\n";
                    textBox2.Text += "name:" + name + "\r\n";
                    
                }
                //textBox2.Text += "Candidates: "+ candidates + "\r\n";

            }
            textBox2.Text += info;

        }
        private async Task<List<Person>> list()
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "3e652220978f4479b3258dd90c71d8ce");

            // Request parameters
            queryString["start"] = "";
            queryString["top"] = "1000";
            var uri = "https://eastasia.api.cognitive.microsoft.com/face/v1.0/persongroups/face03/persons?" + queryString;
            string temp;
            string result;
            //JavaScriptSerializer js = new JavaScriptSerializer();
            var response = await client.GetAsync(uri);
            temp = await response.Content.ReadAsStringAsync();
            
            JArray jArray = JArray.Parse(temp);
            string personId;
            string persistedFaceIds;
            string name="";
            string userData;
            
            foreach (JObject json in jArray)
            {
                personId = json.GetValue("personId").ToString();
                persistedFaceIds = json.GetValue("persistedFaceIds").ToString();
                name = json.GetValue("name").ToString();
                userData = json.GetValue("userData").ToString();
                Person p1 = new Person(personId,name,userData);
                Persons.Add(p1);

            }
            return Persons;
            //Console.WriteLine(Persons.Count);
            


        }
    }
    public class Person
    {
        private string _name; //姓名
        private string _personid;
        private string _userdata;
                                  
        public Person(string personid, string Name,string Userdata)
        {
            this._name = Name;
            this._personid = personid;
            this._userdata = Userdata;
        }
        //姓名
        public string Name
        {
            get { return _name; }
        }
        //年龄
        public string Personid
        {
            get { return _personid; }
        }
        public string Userdata
        {
            get { return _userdata; }
        }
    }
}

