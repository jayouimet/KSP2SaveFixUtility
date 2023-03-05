using System.Security.Cryptography.X509Certificates;
using Accessibility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KSP2SaveFixer
{
    public partial class MainWindow : Form
    {
        // Will hold the path to the folder to open by default
        public string pathToSaveFiles;
        // Will hold the selected save files paths
        public List<string> pathToSaveFilesList;
        // Will hold the selected save files names
        public List<string> nameSaveFilesList;

        public MainWindow()
        {
            InitializeComponent();

            // Default path to campaigns
            this.pathToSaveFiles = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low\\Intercept Games\\Kerbal Space Program 2\\Saves\\SinglePlayer";
            this.textBox1.Text = pathToSaveFiles;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            // Initializing the file system dialog box
            this.openFileDialog1.Multiselect = true;
            this.openFileDialog1.Filter = "Json files (*.json)|*.json";

            // If the directory described in the text box exists, use this path
            if (Directory.Exists(this.textBox1.Text))
            {
                this.openFileDialog1.InitialDirectory = this.textBox1.Text;
            }
            else
            {
                // If not, try to open the default save files path
                if (Directory.Exists(this.pathToSaveFiles))
                {
                    this.openFileDialog1.InitialDirectory = this.pathToSaveFiles;
                }
                // Otherwise, we do not give a default path. This is defaulted to user -> Documents
            }

            // Show the dialog and if the result was OK
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // Save the file names + paths to the above list
                this.pathToSaveFilesList = this.openFileDialog1.FileNames.ToList();
                this.nameSaveFilesList = this.openFileDialog1.SafeFileNames.ToList();
                this.updateUI();
            }
        }

        private void updateUI()
        {
            this.pathListBox.Items.Clear();
            this.pathListBox.Items.AddRange(pathToSaveFilesList.ToArray());
            this.startButton.Enabled = true;
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            // In case no files were selected, we return
            if (this.pathToSaveFilesList is null)
                return;

            // For every file
            for (int i = 0; i < this.pathToSaveFilesList.Count(); i++)
            {
                // Get the file name and path
                string file = this.pathToSaveFilesList[i];
                string safeFileName = this.nameSaveFilesList[i];

                try
                {
                    // Try to read the file
                    string json = File.ReadAllText(file);
                    bool jsonChanged = false;
                    // Deserialize the file into a JObject
                    var values = JsonConvert.DeserializeObject(json);

                    // If not null continue
                    if (values is not null)
                    {
                        // Cast into JObject
                        JObject obj = (JObject)values;
                        // Get value for attribut Vessels
                        var vessels = obj["Vessels"];
                        // For each vessels
                        foreach (var vessel in vessels)
                        {
                            try
                            {
                                // Try to get the CurrentControlOwnerPart Guid
                                string currentControlOwnerPartGuid = vessel["vesselState"]["CurrentControlOwnerPart"]["Guid"].ToString();

                                // If it is not already resetted
                                if (!currentControlOwnerPartGuid.Equals("00000000-0000-0000-0000-000000000000"))
                                {
                                    var parts = vessel["parts"];

                                    bool partIsInVessel = false;

                                    // Itterate through parts
                                    foreach (var part in parts)
                                    {
                                        // If we find the CurrentControlOwnerPart Guid, we set the boolean to true and we break the loop
                                        if (part["PartGuid"]["Guid"].ToString().Equals(currentControlOwnerPartGuid))
                                        {
                                            partIsInVessel = true;
                                            break;
                                        }
                                    }

                                    // If the part was not found, we reset the CurrentControlOwnerPart Guid
                                    if (!partIsInVessel)
                                    {
                                        vessel["vesselState"]["CurrentControlOwnerPart"]["Guid"] = "00000000-0000-0000-0000-000000000000";
                                        // Since a change occured, we keep in mind we need to save the json
                                        jsonChanged = true;
                                    }
                                }
                            }
                            // In case an attribute is missing, just move along. We only care about CurrentControlOwnerPart
                            catch { }
                        }

                        try
                        {
                            // If the json has changed, we try to dump it
                            if (jsonChanged)
                            {
                                // Dump the json
                                string output = JsonConvert.SerializeObject(values, Newtonsoft.Json.Formatting.Indented);
                                File.WriteAllText(file, output);
                                // Show feedback
                                MessageBox.Show(safeFileName + "\nResetted part ownership for invalid vessels.");
                            }
                            else
                            {
                                // Show feedback
                                MessageBox.Show(safeFileName + "\nNo inconsistencies found.");
                            }
                        }
                        catch
                        {
                            // Show feedback if an error occured, could be for many reasons: No write permissions, file is in use, etc
                            MessageBox.Show(safeFileName + "\nCould not overwrite file, skipping.");
                        }
                    }
                }
                // Show feedback if an error occured, could be for many reasons: No read permissions, file is deleted, etc
                catch (Exception ex)
                {
                    MessageBox.Show("Could not open file located at : " + file);
                }
            }
        }
    }
}