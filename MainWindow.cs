using System.Security.Cryptography.X509Certificates;
using Accessibility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KSP2SaveFixer
{
    public partial class MainWindow : Form
    {
        public string pathToSaveFiles;

        public MainWindow()
        {
            InitializeComponent();

            // Default path to campaigns
            this.pathToSaveFiles = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low\\Intercept Games\\Kerbal Space Program 2\\Saves\\SinglePlayer";
            this.textBox1.Text = pathToSaveFiles;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.Multiselect = true;
            this.openFileDialog1.Filter = "Json files (*.json)|*.json";

            if (Directory.Exists(this.pathToSaveFiles))
            {
                this.openFileDialog1.InitialDirectory = this.textBox1.Text;
            }
            else
            {
                if (Directory.Exists(this.pathToSaveFiles))
                {
                    this.openFileDialog1.InitialDirectory = this.pathToSaveFiles;
                }
            }

            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                for (int i = 0; i < this.openFileDialog1.FileNames.Length; i++)
                {
                    string file = this.openFileDialog1.FileNames[i];
                    string safeFileName = this.openFileDialog1.SafeFileNames[i];
                    try
                    {
                        string json = File.ReadAllText(file);
                        bool jsonChanged = false;
                        var values = JsonConvert.DeserializeObject(json);

                        if (values is not null)
                        {
                            JObject obj = (JObject)values;
                            var vessels = obj["Vessels"];
                            foreach (var vessel in vessels)
                            {
                                try
                                {
                                    string currentControlOwnerPartGuid = vessel["vesselState"]["CurrentControlOwnerPart"]["Guid"].ToString();

                                    if (!currentControlOwnerPartGuid.Equals("00000000-0000-0000-0000-000000000000"))
                                    {
                                        var parts = vessel["parts"];

                                        bool partIsInVessel = false;

                                        foreach (var part in parts)
                                        {
                                            if (part["PartGuid"]["Guid"].ToString().Equals(currentControlOwnerPartGuid))
                                            {
                                                partIsInVessel = true;
                                                break;
                                            }
                                        }

                                        if (!partIsInVessel)
                                        {
                                            vessel["vesselState"]["CurrentControlOwnerPart"]["Guid"] = "00000000-0000-0000-0000-000000000000";
                                            jsonChanged = true;
                                        }
                                    }
                                }
                                // In case an attribute is missing, just move along. We only care about CurrentControlOwnerPart
                                catch { }
                            }

                            try
                            {
                                if (jsonChanged)
                                {
                                    // Dump the json
                                    string output = JsonConvert.SerializeObject(values, Newtonsoft.Json.Formatting.Indented);
                                    File.WriteAllText(file, output);
                                    MessageBox.Show(safeFileName + "\nResetted part ownership for invalid vessels.");
                                }
                                else
                                {
                                    MessageBox.Show(safeFileName + "\nNo inconsistencies found.");
                                }
                            }
                            catch
                            {
                                MessageBox.Show(safeFileName + "\nCould not overwrite file, skipping.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not open file located at : " + file);
                    }
                }
            }
        }
    }
}