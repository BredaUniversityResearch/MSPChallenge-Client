using UnityEditor;
using UnityEngine;

public class AssetDatabaseExamples : MonoBehaviour
{
    [MenuItem("AssetDatabase/Import Package Example")]
    public static void ImportPreBuildPackages()
    {
        string packageName = "PrivateClientPlugins.unitypackage";
        var client = new WebClient();

        //user-agent header required to avoid 403 response
        client.Headers.Add("user-agent", " Mozilla/5.0 (Windows NT 6.1; WOW64; rv:25.0) Gecko/20100101 Firefox/25.0");
        //cookie needed to launch the download automatically
        client.Headers.Add("cookie", "FedAuth=77u/PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz48U1A+VjExLDBoLmZ8bWVtYmVyc2hpcHx1cm4lM2FzcG8lM2Fhbm9uIzAzNTI3MWI1MmUxNTUxNTM0YWUyMTJiZmZmYTUyMDhiZjAzNmE5NDhkYzQwZDkzMGQwNzU4MWUxMTNkOGJjODMsMCMuZnxtZW1iZXJzaGlwfHVybiUzYXNwbyUzYWFub24jMDM1MjcxYjUyZTE1NTE1MzRhZTIxMmJmZmZhNTIwOGJmMDM2YTk0OGRjNDBkOTMwZDA3NTgxZTExM2Q4YmM4MywxMzI3NzIyMDIxODAwMDAwMDAsMCwxMzI3NzMwNjMxODY3NjMwNjAsMC4wLjAuMCwyNTgsMGEzMzU4OWItMDAzNi00ZmU4LWE4MjktM2VkMDkyNmFmODg2LCwsMDg5N2YzOWYtOTAyOC0zMDAwLTE0ODAtZTk4NWY5MThkMGQxLDA4OTdmMzlmLTkwMjgtMzAwMC0xNDgwLWU5ODVmOTE4ZDBkMSxydGF6ZG10V0RVZVJ0QkQ1bmlQd253LDAsMCwwLCwsLDI2NTA0Njc3NDM5OTk5OTk5OTksMCwsLCwsLCwwLE91MzBtbXNnTjFnQzdaTFhwQ3h3OGUyUkwybGkwaDF0WUhSdGFIQm1DeG51ZHBmRWUzSWtpMml0WmtpZkdHWkpKKzFNYndiTzRwaThieGJXZFErZUZYdTRsQjZ0RFFEcERRZDFDVXl3V3JldEN5NStHR3BCbEVqVWVRMzJwT28rZ05CZnE2eWY0VG1yQjZiWlA2Y2Q5ejRwUjI2cFg4MjRlRzBoaWliNmxvSWV5TXloOTlCRnFoU0Z3OXhKeDRkTmcyc25oMjlVOTlyWXBaQzN3cUNka3o2ZTk1VHN0eDYzalhMUnhGWklZZjUzL0lWbUxZcWJiRFdMaVFlWWhnaEJsc2crN3MvazdxWVFqUnFxSEdxZTdzM0g4RXViY0x6RVJkS1ZzYlNCeDNRTVpaNTBSUkJiWUprclBSaHRUbittbUhyallmODRoK1JSZisyUXp2V3Fjdz09PC9TUD4=");

        //Downloading the .unitypackage file from OneDrive
        Console.WriteLine("Beginning package download");
        client.DownloadFile("https://edubuas-my.sharepoint.com/:u:/g/personal/fabrini_j_buas_nl/EfffomewNK5JmoGELwOJrugBZwDMDTm9ATvR-9J23-HVzQ?download=1", packageName);
        Console.WriteLine("Package download finished");

        //Importing the package into Unity
        AssetDatabase.ImportPackage(packageName, false);
        
        //Delete the downloaded file
        try
        {
            // Check if file exists with its full path    
            if (File.Exists(packageName))
            {
                // If file found, delete it    
                File.Delete(packageName);
                Console.WriteLine("File deleted.");
            }
            else Console.WriteLine("File not found");
        }
        catch (IOException ioExp)
        {
            Console.WriteLine(ioExp.Message);
        }
    }
}
