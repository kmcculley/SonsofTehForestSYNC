using System.IO;
using System.IO.Compression;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Win32;

const string ServerSaveID = "1573985591"; // lol hardcoded
string chosenSaveDir = "";
string AWSAccessKey = "";
string AWSSecretKey = "";

object? AWSAccessKeyRegistry = Registry.GetValue("HKEY_CURRENT_USER\\Software\\Fatware", "SOTFAWSAccessKey", null);
if (AWSAccessKeyRegistry != null) AWSAccessKey = AWSAccessKeyRegistry.ToString();
while (AWSAccessKeyRegistry == null || AWSAccessKeyRegistry.ToString() == "")
{
    Console.Write("AWS Access Key: ");
    AWSAccessKey = Console.ReadLine() ?? "NADA";
    Registry.SetValue("HKEY_CURRENT_USER\\Software\\Fatware", "SOTFAWSAccessKey", AWSAccessKey);
    AWSAccessKeyRegistry = Registry.GetValue("HKEY_CURRENT_USER\\Software\\Fatware", "SOTFAWSAccessKey", null);
}

object? AWSSecretKeyRegistry = Registry.GetValue("HKEY_CURRENT_USER\\Software\\Fatware", "SOTFAWSSecretKey", null);
if (AWSSecretKeyRegistry != null) AWSSecretKey = AWSSecretKeyRegistry.ToString();
while (AWSSecretKeyRegistry == null || AWSSecretKeyRegistry.ToString() == "")
{
    Console.Write("AWS Secret Key: ");
    AWSSecretKey = Console.ReadLine() ?? "NADA";
    Registry.SetValue("HKEY_CURRENT_USER\\Software\\Fatware", "SOTFAWSSecretKey", AWSSecretKey);
    AWSSecretKeyRegistry = Registry.GetValue("HKEY_CURRENT_USER\\Software\\Fatware", "SOTFAWSSecretKey", null);
}



object? SaveDirectoryRegistry = Registry.GetValue("HKEY_CURRENT_USER\\Software\\Fatware", "SOTFSaveDir", null);
if (SaveDirectoryRegistry != null) chosenSaveDir = SaveDirectoryRegistry.ToString();
if (SaveDirectoryRegistry == null || SaveDirectoryRegistry.ToString() == "")
{
    string saveLocation = Environment.GetEnvironmentVariable("UserProfile") + @"\AppData\LocalLow\Endnight\SonsOfTheForest\Saves\";
    string[] saveLocationDirs = Directory.GetDirectories(saveLocation);
    if (saveLocationDirs.Length == 0)
    {
        Console.WriteLine("Your computer sucks, theres no saves");
        Console.ReadLine();
        Environment.Exit(0);
    }
    else if (saveLocationDirs.Length > 1)
    {
        Console.WriteLine("You got more than one folder for some dumb reason... ");
        for (int i = 0; i < saveLocationDirs.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {saveLocationDirs[i]}");
        }
        Console.WriteLine("");
        Console.WriteLine("Choose which directory to use.");

        int selection;
        while (!int.TryParse(Console.ReadLine(), out selection) || selection < 1 || selection > saveLocationDirs.Length)
        {
            Console.WriteLine("STFU. Choose which directory to use.");
        }
        chosenSaveDir = saveLocationDirs[selection - 1];
        Registry.SetValue("HKEY_CURRENT_USER\\Software\\Fatware", "SOTFSaveDir", chosenSaveDir);
        //Console.WriteLine(saveLocationDirs[selection - 1]);
    }
    else
    {
        chosenSaveDir = saveLocationDirs[0];
        Registry.SetValue("HKEY_CURRENT_USER\\Software\\Fatware", "SOTFSaveDir", chosenSaveDir);
    }

}

string zipFilePath = chosenSaveDir + @"\" + ServerSaveID + ".zip";

Console.WriteLine();
Console.WriteLine("What do you want to do?");
Console.WriteLine();
Console.WriteLine("1. I want to HOST the server. No one else is currently hosting.");
Console.WriteLine("2. The last time I played, I was the host. Now I am going to join someone else who is the host.");
Console.WriteLine("3. I just got done HOSTING and now I'm gonna quit cause I suck.");
Console.WriteLine("4. I wasn't the host last time. I'm just going to join. WTF did I launch this then?");
Console.WriteLine("5. Reset of this app's settings.");
Console.WriteLine();

int selection2;
while (!int.TryParse(Console.ReadLine(), out selection2) || selection2 < 1 || selection2 > 5)
{
    Console.WriteLine("You're dumb, choose again.");
}

if (selection2 == 1)
{
    BackupRoutine(chosenSaveDir);
    // Download from S3
    Console.WriteLine("Downloading Server Files...");
    TransferServerFiles(1, zipFilePath, AWSAccessKey.ToString(), AWSSecretKey.ToString());
    // Copy Player Data to Server
    Console.WriteLine("Extracting Server Files...");
    ZipFile.ExtractToDirectory(zipFilePath, chosenSaveDir + @"\Multiplayer\", true);
    Console.WriteLine("Copying player data...");
    CopyPlayerDataToServer(chosenSaveDir);
    Console.WriteLine("Done, close this and go play omg.");
    Console.ReadLine();
    Environment.Exit(0);
}
else if (selection2 == 2)
{
    BackupRoutine(chosenSaveDir);
    // Copy Player Data To Client
    Console.WriteLine("Copying player data...");
    CopyPlayerDataToClient(chosenSaveDir);
    Console.WriteLine("Done, close this and go play omg.");
    Console.ReadLine();
    Environment.Exit(0);
}
else if (selection2 == 3)
{
    BackupRoutine(chosenSaveDir);
    Console.WriteLine("Zipping Server Files...");
    File.Delete(zipFilePath);
    ZipFile.CreateFromDirectory(chosenSaveDir + @"\Multiplayer\" + ServerSaveID, zipFilePath);
    // Upload to S3
    Console.WriteLine("Uploading Server Files...");
    TransferServerFiles(2, zipFilePath, AWSAccessKey.ToString(), AWSSecretKey.ToString());
    // Copy Player Data to Client
    Console.WriteLine("Copying player data...");
    CopyPlayerDataToClient(chosenSaveDir);
    Console.WriteLine("Its done. You can close this now, bye.");
    Console.ReadLine();
    Environment.Exit(0);
}
else if (selection2 == 5)
{
    Registry.SetValue("HKEY_CURRENT_USER\\Software\\Fatware", "SOTFAWSAccessKey", "");
    Registry.SetValue("HKEY_CURRENT_USER\\Software\\Fatware", "SOTFAWSSecretKey", "");
    Registry.SetValue("HKEY_CURRENT_USER\\Software\\Fatware", "SOTFSaveDir", "");
    Console.WriteLine("Everything is reset. Relaunch the app.");
    Console.ReadLine();
    Environment.Exit(0);
}
else
{
    Console.WriteLine("Just go play the game then, bye.");
    Console.ReadLine();
    Environment.Exit(0);
}





static void CopyPlayerDataToClient(string ChosenSaveDir)
{
    var filesToCopy = new DirectoryInfo(ChosenSaveDir + @"\Multiplayer\" + ServerSaveID).GetFiles("Player*").Select(file => file.FullName);
    var filesToCopy2 = new DirectoryInfo(ChosenSaveDir + @"\Multiplayer\" + ServerSaveID).GetFiles("GameStateSaveDat*").Select(file => file.FullName);
    var filesToCopy3 = new DirectoryInfo(ChosenSaveDir + @"\Multiplayer\" + ServerSaveID).GetFiles("SaveDat*").Select(file => file.FullName);

    string targetDir = GetLastModifiedDirectory(ChosenSaveDir + @"\MultiplayerClient");
    foreach (string fileToCopy in filesToCopy)
    {
        string destination = Path.Combine(targetDir, Path.GetFileName(fileToCopy));

        File.Copy(fileToCopy, destination, true);
    }
    string destination2 = Path.Combine(targetDir, Path.GetFileName(filesToCopy2.First()));
    string destination3 = Path.Combine(targetDir, Path.GetFileName(filesToCopy3.First()));

    File.Copy(filesToCopy2.First(), destination2, true);
    File.Copy(filesToCopy3.First(), destination3, true);

}

static void CopyPlayerDataToServer(string ChosenSaveDir)
{
    string sourceDir = GetLastModifiedDirectory(ChosenSaveDir + @"\MultiplayerClient");
    var filesToCopy = new DirectoryInfo(sourceDir).GetFiles("Player*").Select(file => file.FullName);

    string targetDir = ChosenSaveDir + @"\Multiplayer\" + ServerSaveID;
    foreach (string fileToCopy in filesToCopy)
    {
        string destination = Path.Combine(targetDir, Path.GetFileName(fileToCopy));

        File.Copy(fileToCopy, destination, true);
    }
}

static string GetLastModifiedDirectory(string ParentDirectory)
{
    string[] subDirs = Directory.GetDirectories(ParentDirectory);
    string? lastModifiedSubDir = subDirs.OrderByDescending(dir => Directory.GetLastWriteTime(dir)).FirstOrDefault();
    if (lastModifiedSubDir != null)
        return lastModifiedSubDir;

    return "";


}








static void TransferServerFiles(int TransferType, string ZipFilePath, string accessKey, string secretKey)
{
    string bucketName = "sonsoftehforestsaves";



    TransferUtility transferUtility = new TransferUtility(new AmazonS3Client(accessKey, secretKey, RegionEndpoint.USEast1));

    if (TransferType == 1) // Download
    {
        File.Delete(ZipFilePath);
        transferUtility.Download(ZipFilePath, bucketName, ServerSaveID + ".zip");
    }
    else if (TransferType == 2) 
    {
        transferUtility.Upload(ZipFilePath, bucketName, ServerSaveID + ".zip");
    }
}


// need to zip and unzip files, then tie in the transfer.

static void BackupRoutine(string SavePath)
{
    ZipFile.CreateFromDirectory(SavePath + @"\Multiplayer\" + ServerSaveID, Directory.GetParent(SavePath) + @"\Backup-" + DateTime.Now.ToString().Replace('/', '-').Replace(':', '-') + ".zip");
}