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
        private int objID;
        private int newObjID;
        private bool isCurrent;

        private async void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!client.connected)
            {
                client = new AsyncSocket();
                await client.Connect("127.0.0.1", 33666);
                if (client.connected)
                {
                    connectToolStripMenuItem.Text = "Disconnect";
                    StatusStrip.Text = "Connected!";
                    StatusStrip.BackColor = Color.DodgerBlue;
                    statusStrip1.BackColor = Color.DodgerBlue;
                    StatusStrip.ForeColor = Color.White;
                    await client.SendAsync("<inspect_plan_info />");
                    var rcvdata = ReceiveDataAsync();
                }
                else
                {
                    connectToolStripMenuItem.Text = "Retry";
                    StatusStrip.Text = "Unable To Connect!";
                    StatusStrip.BackColor = Color.Crimson;
                    statusStrip1.BackColor = Color.Crimson;
                    StatusStrip.ForeColor = Color.White;

                }
            }
            else
            {
                client.Disconnect();
                connectToolStripMenuItem.Text = "Connect";
                StatusStrip.Text = "Disconnected!";
                StatusStrip.BackColor = Color.Silver;
                statusStrip1.BackColor = Color.Silver;
                StatusStrip.ForeColor = Color.Black;
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
            if (xmlresponse.Contains("<acknowledgement />") || (xmlresponse.Contains("<!--Verisurf Server Welcome Message-->\n") || (String.IsNullOrWhiteSpace(xmlresponse))))
                return;

            //Initial Entry
            if (xmlresponse.Contains("inspect_plan_info"))
            {
                BeginSearchLoop(xmlresponse);
                return;
            }

            //Check if Last Feature has Measured Value and Get Results 
            if (xmlresponse.Contains("inspect_object_info"))
            {
                if (xmlresponse.Contains("measured"))
                {
                    if (newObjID != objID | newObjID == 0)
                    {

                        //DROLayout.Controls.Clear();
                        //DROLayout.RowCount = 0;

                        //DROLayout.RowStyles.Clear();

                        XmlDocument xml = new XmlDocument();
                        xml.LoadXml(xmlresponse);
                        DROLayout.Controls.Add(featureLabel, 0, 0);
                        DROLayout.Controls.Add(label2, 2, 0);
                        DROLayout.Controls.Add(label3, 3, 0);
                        DROLayout.Controls.Add(label1, 1, 0);
                        string newNodeID = xml["response"]["success"]["data"]["inspect_object_info"]["object"].Attributes["id"].Value;
                        newObjID = Convert.ToInt32(newNodeID);
                        featureLabel.Text = xml["response"]["success"]["data"]["inspect_object_info"]["object"].InnerText;
                        GetText(xml.SelectNodes("response/success/data/inspect_object_info/object/property"));
                        SendMeasure();
                        DeleteRows();              
                    }      
                     else 
                    {
                        SendMeasure();
              
                    }
                }

                //If last Plan Object does not have Measured Features, decrement "lastFeature" until found and run "inspect_object_info"
                else
                {
                    DecrementObjID(xmlresponse);

                }
            }
        }

        //Parses VS Xml data to find the last Object ID in the Active plan.
        private async void BeginSearchLoop(string allFeatures)
        {
            var doc = XDocument.Parse(allFeatures);
            var last = doc.Descendants("plan_object").Last();
            objID = Convert.ToInt32(last.LastAttribute.Value);
            await client.SendAsync($"<inspect_object_info id=\"{ objID }\" />");
            
        }

        //Decrements the last object ID in Plan, Loops until xmlresponse contains measured
        private async void DecrementObjID(string xmlresponse)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(xmlresponse);
            string ObjectNodeID = xml["response"]["success"]["data"]["inspect_object_info"]["object"].Attributes["id"].Value;

            if (ObjectNodeID != null)
            {
                int lastVal = Convert.ToInt32(ObjectNodeID);

                if (lastVal >= 0)
                {
                    objID = lastVal - 1;
                    await client.SendAsync($"<inspect_object_info id=\"{ objID }\" />");

                }                       
            }
        }

        private void GetText(XmlNodeList properties)
        {

            DROLayout.SuspendLayout();

            foreach (XmlNode property in properties)
            {
                if (property.Attributes["name"].Value == null)
                {
                    return;
                }
                if (property.Attributes["name"].Value == "X")
                {
                    DROLayout.Controls.Add(xNameLabel, 0, 2);
                    DROLayout.Controls.Add(xActLabelVal, 1, 2);
                    DROLayout.Controls.Add(xNomLabelVal, 2, 2);
                    DROLayout.Controls.Add(xDLabelVal, 3, 2);
                    double inTolPlus = Double.Parse(property.Attributes["tolmax"].InnerText);
                    double inTolMinus = Double.Parse(property.Attributes["tolmin"].InnerText);
                    double devVal = Double.Parse(property.Attributes["deviation"].InnerText);
                    xDLabelVal.Text = (double.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");
                    xNomLabelVal.Text = (double.Parse(property.Attributes["nominal"].InnerText)).ToString("F4");
                    xActLabelVal.Text = (double.Parse(property.Attributes["measured"].InnerText)).ToString("F4");
                    if ((devVal > inTolPlus) || (devVal < inTolMinus))

                    {
                        xDLabelVal.ForeColor = Color.Crimson;
                        xNomLabelVal.ForeColor = Color.Crimson;
                        xActLabelVal.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        xDLabelVal.ForeColor = Color.ForestGreen;
                        xNomLabelVal.ForeColor = Color.ForestGreen;
                        xActLabelVal.ForeColor = Color.ForestGreen;
                    }
                }
                if (property.Attributes["name"].Value == "Y")
                {
                    DROLayout.Controls.Add(yNameLabel, 0, 3);
                    DROLayout.Controls.Add(yActLabelVal, 1, 3);
                    DROLayout.Controls.Add(yNomLabelVal, 2, 3);
                    DROLayout.Controls.Add(yDLabelVal, 3, 3);

                    double inTolPlus = Double.Parse(property.Attributes["tolmax"].InnerText);
                    double inTolMinus = Double.Parse(property.Attributes["tolmin"].InnerText);
                    double devVal = Double.Parse(property.Attributes["deviation"].InnerText);

                    yDLabelVal.Text = (double.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");
                    yActLabelVal.Text = (double.Parse(property.Attributes["measured"].InnerText)).ToString("F4");
                    yNomLabelVal.Text = (double.Parse(property.Attributes["nominal"].InnerText)).ToString("F4");

                    if ((devVal > inTolPlus) || (devVal < inTolMinus))
                    {
                        yDLabelVal.ForeColor = Color.Crimson;
                        yActLabelVal.ForeColor = Color.Crimson;
                        yNomLabelVal.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        yDLabelVal.ForeColor = Color.ForestGreen;
                        yNomLabelVal.ForeColor = Color.ForestGreen;
                        yActLabelVal.ForeColor = Color.ForestGreen;
                    }
                }
                if (property.Attributes["name"].Value == "Z")
                {
                    DROLayout.Controls.Add(zActLabelVal, 1, 4);
                    DROLayout.Controls.Add(zNomLabelVal, 2, 4);
                    DROLayout.Controls.Add(zNameLabel, 0, 4);
                    DROLayout.Controls.Add(zDLabelVal, 3, 4);

                    double inTolPlus = Double.Parse(property.Attributes["tolmax"].InnerText);
                    double inTolMinus = Double.Parse(property.Attributes["tolmin"].InnerText);
                    double devVal = Double.Parse(property.Attributes["deviation"].InnerText);

                    zDLabelVal.Text = (Double.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");
                    zActLabelVal.Text = (Double.Parse(property.Attributes["measured"].InnerText)).ToString("F4");
                    zNomLabelVal.Text = (Double.Parse(property.Attributes["nominal"].InnerText)).ToString("F4");
                    if ((devVal > inTolPlus) || (devVal < inTolMinus))
                    {
                        zDLabelVal.ForeColor = Color.Crimson;
                        zActLabelVal.ForeColor = Color.Crimson;
                        zNomLabelVal.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        zDLabelVal.ForeColor = Color.ForestGreen;
                        zNomLabelVal.ForeColor = Color.ForestGreen;
                        zActLabelVal.ForeColor = Color.ForestGreen;
                    }
                }
                if (property.Attributes["name"].Value == "Angle")
                {
                    DROLayout.Controls.Add(angDevVal, 3, 12);
                    DROLayout.Controls.Add(angActVal, 1, 12);
                    DROLayout.Controls.Add(angNomVal, 2, 12);
                    DROLayout.Controls.Add(angleNameLabel, 0, 12);

                    double inTolPlus = Double.Parse(property.Attributes["tolmax"].InnerText);
                    double inTolMinus = Double.Parse(property.Attributes["tolmin"].InnerText);
                    double devVal = Double.Parse(property.Attributes["deviation"].InnerText);

                    angDevVal.Text = (double.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");
                    angNomVal.Text = (double.Parse(property.Attributes["nominal"].InnerText)).ToString("F4");
                    angActVal.Text = (double.Parse(property.Attributes["measured"].InnerText)).ToString("F4");

                    if ((devVal > inTolPlus) || (devVal < inTolMinus))
                    {
                        angDevVal.ForeColor = Color.Crimson;
                        angNomVal.ForeColor = Color.Crimson;
                        angActVal.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        angDevVal.ForeColor = Color.ForestGreen;
                        angNomVal.ForeColor = Color.ForestGreen;
                        angActVal.ForeColor = Color.ForestGreen;
                    }
                }
                if (property.Attributes["name"].Value == "Diameter")
                {
                    DROLayout.Controls.Add(diaNomLabelVal, 2, 7);
                    DROLayout.Controls.Add(diaDevLabelVal, 3, 7);
                    DROLayout.Controls.Add(diaNameLabel, 0, 7);
                    DROLayout.Controls.Add(diaActLabelVal, 1, 7);

                    double inTolPlus = Double.Parse(property.Attributes["tolmax"].InnerText);
                    double inTolMinus = Double.Parse(property.Attributes["tolmin"].InnerText);
                    double devVal = Double.Parse(property.Attributes["deviation"].InnerText);

                    diaDevLabelVal.Text = (double.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");
                    diaNomLabelVal.Text = (double.Parse(property.Attributes["nominal"].InnerText)).ToString("F4");
                    diaActLabelVal.Text = (double.Parse(property.Attributes["measured"].InnerText)).ToString("F4");

                    if ((devVal > inTolPlus) || (devVal < inTolMinus))
                    {
                        diaDevLabelVal.ForeColor = Color.Crimson;
                        diaNomLabelVal.ForeColor = Color.Crimson;
                        diaActLabelVal.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        diaDevLabelVal.ForeColor = Color.ForestGreen;
                        diaNomLabelVal.ForeColor = Color.ForestGreen;
                        diaActLabelVal.ForeColor = Color.ForestGreen;
                    }
                }
                if (property.Attributes["name"].Value == "Position")
                {
                    DROLayout.Controls.Add(posNomLabelVal, 2, 8);
                    DROLayout.Controls.Add(posDevLabelVal, 3, 8);
                    DROLayout.Controls.Add(posNameLabel, 0, 8);
                    DROLayout.Controls.Add(posActLabelVal, 1, 8);

                    double inTolPlus = Double.Parse(property.Attributes["tolmax"].InnerText);
                    double measVal = Double.Parse(property.Attributes["measured"].InnerText);

                    posActLabelVal.Text = (double.Parse(property.Attributes["measured"].InnerText)).ToString("F4");
                    posDevLabelVal.Text = (double.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");
                    posNomLabelVal.Text = (double.Parse(property.Attributes["nominal"].InnerText)).ToString("F4");

                    if (measVal > inTolPlus)
                    {
                        posDevLabelVal.ForeColor = Color.Crimson;
                        posActLabelVal.ForeColor = Color.Crimson;
                        posNomLabelVal.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        posDevLabelVal.ForeColor = Color.ForestGreen;
                        posNomLabelVal.ForeColor = Color.ForestGreen;
                        posActLabelVal.ForeColor = Color.ForestGreen;
                    }
                }
                if (property.Attributes["name"].Value == "MMC")
                {
                    DROLayout.Controls.Add(mmcDevVal, 3, 14);
                    DROLayout.Controls.Add(mmcActVal, 1, 14);
                    DROLayout.Controls.Add(mmcNomVal, 2, 14);
                    DROLayout.Controls.Add(mmcNameLabel, 0, 14);

                    double inTolPlus = Double.Parse(property.Attributes["tolmax"].InnerText);
                    double devVal = Double.Parse(property.Attributes["deviation"].InnerText);

                    mmcDevVal.Text = (double.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");
                    mmcActVal.Text = (double.Parse(property.Attributes["measured"].InnerText)).ToString("F4");
                    mmcNomVal.Text = (double.Parse(property.Attributes["nominal"].InnerText)).ToString("F4");

                    if (devVal > inTolPlus)
                    {
                        mmcDevVal.ForeColor = Color.Crimson;
                        mmcActVal.ForeColor = Color.Crimson;
                        mmcNomVal.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        mmcDevVal.ForeColor = Color.ForestGreen;
                        mmcNomVal.ForeColor = Color.ForestGreen;
                        mmcActVal.ForeColor = Color.ForestGreen;
                    }
                }
                if (property.Attributes["name"].Value == "LMC")
                {
                    DROLayout.Controls.Add(lmcNomVal, 2, 13);
                    DROLayout.Controls.Add(lmcDevVal, 3, 13);
                    DROLayout.Controls.Add(lmcActVal, 1, 13);
                    DROLayout.Controls.Add(lmcNameLabel, 0, 13);

                    double inTolPlus = Double.Parse(property.Attributes["tolmax"].InnerText);
                    double devVal = Double.Parse(property.Attributes["deviation"].InnerText);

                    lmcDevVal.Text = (double.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");
                    lmcActVal.Text = (double.Parse(property.Attributes["measured"].InnerText)).ToString("F4");
                    lmcNomVal.Text = (double.Parse(property.Attributes["nominal"].InnerText)).ToString("F4");

                    if (devVal > inTolPlus)
                    {
                        lmcDevVal.ForeColor = Color.Crimson;
                        lmcActVal.ForeColor = Color.Crimson;
                        lmcNomVal.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        lmcDevVal.ForeColor = Color.ForestGreen;
                        lmcNomVal.ForeColor = Color.ForestGreen;
                        lmcActVal.ForeColor = Color.ForestGreen;
                    }
                }
                if (property.Attributes["name"].Value == "Width")
                {
                    DROLayout.Controls.Add(widthDevLabelVal, 3, 10);
                    DROLayout.Controls.Add(widthActLabelVal, 1, 10);
                    DROLayout.Controls.Add(widthNomLabelVal, 2, 10);
                    DROLayout.Controls.Add(widthNameLabel, 0, 10);

                    double inTolPlus = Double.Parse(property.Attributes["tolmax"].InnerText);
                    double inTolMinus = Double.Parse(property.Attributes["tolmin"].InnerText);
                    double devVal = Double.Parse(property.Attributes["deviation"].InnerText);

                    widthDevLabelVal.Text = (double.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");
                    widthActLabelVal.Text = (double.Parse(property.Attributes["measured"].InnerText)).ToString("F4");
                    widthNomLabelVal.Text = (double.Parse(property.Attributes["nominal"].InnerText)).ToString("F4");
                    if ((devVal > inTolPlus) || (devVal < inTolMinus))
                    {
                        widthDevLabelVal.ForeColor = Color.Crimson;
                        widthActLabelVal.ForeColor = Color.Crimson;
                        widthNomLabelVal.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        widthDevLabelVal.ForeColor = Color.ForestGreen;
                        widthNomLabelVal.ForeColor = Color.ForestGreen;
                        widthActLabelVal.ForeColor = Color.ForestGreen;
                    }
                }
                if (property.Attributes["name"].Value == "Distance")
                {
                    DROLayout.Controls.Add(distanceNameLabel, 0, 11);
                    DROLayout.Controls.Add(distActLabelVal, 1, 11);
                    DROLayout.Controls.Add(distNomLabelVal, 2, 11);
                    DROLayout.Controls.Add(distDevLabelVal, 3, 11);

                    double inTolPlus = Double.Parse(property.Attributes["tolmax"].InnerText);
                    double devVal = Double.Parse(property.Attributes["deviation"].InnerText);

                    distDevLabelVal.Text = (double.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");
                    distActLabelVal.Text = (double.Parse(property.Attributes["measured"].InnerText)).ToString("F4");
                    distNomLabelVal.Text = (double.Parse(property.Attributes["nominal"].InnerText)).ToString("F4");

                    if (devVal > inTolPlus)
                    {
                        distDevLabelVal.ForeColor = Color.Crimson;
                        distActLabelVal.ForeColor = Color.Crimson;
                        distNomLabelVal.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        distDevLabelVal.ForeColor = Color.ForestGreen;
                        distNomLabelVal.ForeColor = Color.ForestGreen;
                        distActLabelVal.ForeColor = Color.ForestGreen;
                    }
                }
                if (property.Attributes["name"].Value == "Length")
                {
                    DROLayout.Controls.Add(lengthNomLabelVal, 2, 9);
                    DROLayout.Controls.Add(lengthDevLabelVal, 3, 9);
                    DROLayout.Controls.Add(lengthNameVal, 0, 9);
                    DROLayout.Controls.Add(lengthActLabelVal, 1, 9);

                    double inTolPlus = Double.Parse(property.Attributes["tolmax"].InnerText);
                    double devVal = Double.Parse(property.Attributes["deviation"].InnerText);

                    lengthDevLabelVal.Text = (double.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");
                    lengthActLabelVal.Text = (double.Parse(property.Attributes["measured"].InnerText)).ToString("F4");
                    lengthNomLabelVal.Text = (double.Parse(property.Attributes["nominal"].InnerText)).ToString("F4");

                    if (devVal > inTolPlus)
                    {
                        lengthDevLabelVal.ForeColor = Color.Crimson;
                        lengthActLabelVal.ForeColor = Color.Crimson;
                        lengthNomLabelVal.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        lengthDevLabelVal.ForeColor = Color.ForestGreen;
                        lengthNomLabelVal.ForeColor = Color.ForestGreen;
                        lengthActLabelVal.ForeColor = Color.ForestGreen;
                    }
                }
                if (property.Attributes["name"].Value == "3D Position")
                {
                    DROLayout.Controls.Add(dActLabelVal, 1, 5);
                    DROLayout.Controls.Add(dDevLabelVal, 3, 5);
                    DROLayout.Controls.Add(dNomLabelVal, 2, 5);
                    DROLayout.Controls.Add(dNameLabel, 0, 5);

                    double inTolPlus = Double.Parse(property.Attributes["tolmax"].InnerText);
                    double inTolMinus = Double.Parse(property.Attributes["tolmin"].InnerText);
                    double devVal = Double.Parse(property.Attributes["deviation"].InnerText);

                    dDevLabelVal.Text = (double.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");
                    dActLabelVal.Text = (double.Parse(property.Attributes["measured"].InnerText)).ToString("F4");
                    dNomLabelVal.Text = (double.Parse(property.Attributes["nominal"].InnerText)).ToString("F4");

                    if ((devVal > inTolPlus) || (devVal < inTolMinus))
                    {
                        dDevLabelVal.ForeColor = Color.Crimson;
                        dActLabelVal.ForeColor = Color.Crimson;
                        dNomLabelVal.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        dDevLabelVal.ForeColor = Color.ForestGreen;
                        dNomLabelVal.ForeColor = Color.ForestGreen;
                        dActLabelVal.ForeColor = Color.ForestGreen;
                    }
                }
                if (property.Attributes["name"].Value == "Roundness" || property.Attributes["name"].Value == "Flatness" || property.Attributes["name"].Value == "Sphericity" || property.Attributes["name"].Value == "Cylindricity" || property.Attributes["name"].Value == "Conicity" || property.Attributes["name"].Value == "Straightness" || property.Attributes["name"].Value == "Form")
                {
                    DROLayout.Controls.Add(formDevLabelVal, 3, 6);
                    DROLayout.Controls.Add(formNomLabel, 2, 6);
                    DROLayout.Controls.Add(formNameLabel, 0, 6);
                    DROLayout.Controls.Add(formActLabel, 1, 6);

                    double inTolPlus = Double.Parse(property.Attributes["tolmax"].InnerText);
                    double devVal = Double.Parse(property.Attributes["deviation"].InnerText);

                    formDevLabelVal.Text = (double.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");
                    formActLabel.Text = (double.Parse(property.Attributes["deviation"].InnerText)).ToString("F4");
                    formNomLabel.Text = ("0.000");

                    if (devVal > inTolPlus)
                    {
                        formDevLabelVal.ForeColor = Color.Crimson;
                        formActLabel.ForeColor = Color.Crimson;
                        formNomLabel.ForeColor = Color.Crimson;
                    }
                    else
                    {
                        formDevLabelVal.ForeColor = Color.ForestGreen;
                        formNomLabel.ForeColor = Color.ForestGreen;
                        formActLabel.ForeColor = Color.ForestGreen;
                    }
                }
            }

            DROLayout.ResumeLayout();            
        }

        private void DeleteRows()
        {
            for (int row = DROLayout.RowCount - 1; row >= 0; row--)
            {
                bool hasControl = false;
                for (int col = 0; col < DROLayout.ColumnCount; col++)
                {
                    if (DROLayout.GetControlFromPosition(col, row) != null)
                    {
                        hasControl = true;
                        break;
                    }
                }

                if (!hasControl)
                {
                   // DROLayout.RowStyles.RemoveAt(row);
                    DROLayout.RowCount--;
                }
            }
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (client.connected)
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog
                {
                    Filter = "Verisurf Files (*.mcam)|*.*"
                };
                if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    await client.SendAsync("<file_open filename='" + openFileDialog1.FileName + "' />");
                }
            }
        }

        private async void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (client.connected)
            {
                SaveFileDialog saveFileDialog1 = new SaveFileDialog
                {
                    Filter = "Verisurf Files (*.mcam)|*.*"
                };
                if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    await client.SendAsync("<file_save filename='" + saveFileDialog1.FileName + "' />");
                }
            }
        }

        //private async void loopTimer_Tick(object sender, EventArgs e)
        //{
        //    {
        //        await client.SendAsync("<inspect_plan_info />");
        //    }
        //}
        private async void SendMeasure()
        {
            if (client.connected)
            {
                await Task.Delay(1000);
                await client.SendAsync("<inspect_plan_info />");

            }

        }


    }
}