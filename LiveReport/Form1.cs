using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace LiveReport
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private AsyncSocket client = new AsyncSocket();
        public bool runstate = false;

        private async void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!client.connected)
            {
                client = new AsyncSocket();
                await client.Connect("127.0.0.1", 33666);
                if (client.connected)
                {
                    connectToolStripMenuItem.Text = "Disconnect";
                    var rcvdata = ReceiveDataAsync();
                }
                else
                {
                    connectToolStripMenuItem.Text = "Retry";
                }
            }
            else
            {
                client.Disconnect();
                connectToolStripMenuItem.Text = "Connect";
            }
        }

        private async Task ReceiveDataAsync()
        {
            string data = string.Empty;
            while (client.connected)
            {
                try
                {
                    data = await client.ReadLineAsync();
                    ParseResponseAsync(data);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"RCV Data: {ex.GetType()}: {ex.Message}");
                }
            }
        }

        private void ParseResponseAsync(string xmlresponse)
        {
            if (string.IsNullOrWhiteSpace(xmlresponse))
                return;

            if (xmlresponse.Contains("<!--Verisurf Server Welcome Message-->\n"))
                return;

            if (xmlresponse.Contains("<acknowledgement />"))
                return;

            if (xmlresponse.Contains("plan_object"))
            {
                sendMeasure(xmlresponse);
            }

            if (xmlresponse.Contains("inspect_object_info"))
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(xmlresponse);

                GetText(xml.SelectNodes("response/success/data/inspect_object_info/object/property"));
            }
        }

        private void GetText(XmlNodeList properties)
        {
            foreach (XmlNode property in properties)
            {
                if (property.Attributes["name"].Value == null)
                {
                    return;
                }
                float devVal = float.Parse(property.Attributes["deviation"].InnerText);
                float inTolPlus = float.Parse(property.Attributes["tolmax"].InnerText);
                float inTolMinus = float.Parse(property.Attributes["tolmin"].InnerText);

                if (property.Attributes["name"].Value == "X")
                {
               
                    this.tableLayoutPanel1.Controls.Add(this.dxLabelName, 0, 4);
                    this.tableLayoutPanel1.Controls.Add(this.dxLabel, 1, 4);
                    dxLabel.Text = (float.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");

                    if ((devVal > inTolPlus) || (devVal < inTolMinus))

                    {
                        dxLabel.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        dxLabel.ForeColor = Color.ForestGreen;
                    }
                }

                if (property.Attributes["name"].Value == "Y")
                {

                    this.tableLayoutPanel1.Controls.Add(this.dyLabelName, 0, 7);
                    this.tableLayoutPanel1.Controls.Add(this.dyLabel, 1, 7);
                    dyLabel.Text = (float.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");
                    if ((devVal > inTolPlus) || (devVal < inTolMinus))
                    {
                        dyLabel.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        dyLabel.ForeColor = Color.ForestGreen;
                    }
                }
                if (property.Attributes["name"].Value == "Z")
                {

                    this.tableLayoutPanel1.Controls.Add(this.dzLabel, 1, 8);
                    this.tableLayoutPanel1.Controls.Add(this.dzLabelName, 0, 8);
                    dzLabel.Text = (float.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");
                    if ((devVal > inTolPlus) || (devVal < inTolMinus))
                    {
                        dzLabel.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        dzLabel.ForeColor = Color.ForestGreen;
                    }
                }
                if (property.Attributes["name"].Value == "Angle")
                {

                    this.tableLayoutPanel1.Controls.Add(this.angleLabel, 1, 3);
                    this.tableLayoutPanel1.Controls.Add(this.angleLabelName, 0, 3);
                    angleLabel.Text = (float.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");
                    if ((devVal > inTolPlus) || (devVal < inTolMinus))
                    {
                        angleLabel.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        angleLabel.ForeColor = Color.ForestGreen;
                    }
                }
                if (property.Attributes["name"].Value == "Diameter")
                {

                    this.tableLayoutPanel1.Controls.Add(this.diaLabel, 1, 0);
                    this.tableLayoutPanel1.Controls.Add(this.diaLabelName, 0, 0);
                    diaLabel.Text = (float.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");
                    if ((devVal > inTolPlus) || (devVal < inTolMinus))
                    {
                        diaLabel.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        diaLabel.ForeColor = Color.ForestGreen;
                    }
                }
                if (property.Attributes["name"].Value == "Position")
                {
                    float measVal = float.Parse(property.Attributes["measured"].InnerText);
                    this.tableLayoutPanel1.Controls.Add(this.posLabel, 1, 1);
                    this.tableLayoutPanel1.Controls.Add(this.PosLabelName, 0, 1);
                    posLabel.Text = (float.Parse(property.Attributes["measured"].InnerText)).ToString("F4");
                    if (measVal > inTolPlus)
                    {
                        posLabel.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        posLabel.ForeColor = Color.ForestGreen;
                    }
                }
                if (property.Attributes["name"].Value == "Roundness")
                {
                    tableLayoutPanel1.Controls.Add(this.formLabelName, 0, 2);
                    tableLayoutPanel1.Controls.Add(this.formLabel, 1, 2);
                    formLabel.Text = (float.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");
                    if (devVal > inTolPlus)
                    {
                        formLabel.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        formLabel.ForeColor = Color.ForestGreen;
                    }
                }
            }
        }

        private async void sendMeasure(string activeFeature)
        {
            var doc = XDocument.Parse(activeFeature);
            var last = doc.Descendants("plan_object").Last();
            var lastFeature = last.LastAttribute.Value;
            var lastFeatureName = last.Value;
            featureLabel.Text = lastFeatureName;
            await client.SendAsync($"<inspect_object_info id=\"{lastFeature }\" />");
        }

        private async void timer1_Tick(object sender, EventArgs e)
        {
            if (client.connected)
                await client.SendAsync("<inspect_plan_info />");
        }

        public static float NewFontSize(Graphics graphics, Size size, Font font, string str)
        {
            SizeF stringSize = graphics.MeasureString(str, font);
            float wRatio = size.Width / stringSize.Width;
            float hRatio = size.Height / stringSize.Height;
            float ratio = Math.Min(hRatio, wRatio);
            return font.Size * ratio;
        }
    }
}