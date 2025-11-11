using System.IO;
using System.Text;
using UnityEngine;

public class ListSoundFiles : MonoBehaviour
{
    void Start()
    {
        string folderPath = @"C:\Users\nam\Documents\GitHub\Color-Jelly-Puzzle\Assets\Sounds";

        if (!Directory.Exists(folderPath))
        {
            Debug.LogError("Thư mục không tồn tại: " + folderPath);
            return;
        }

        // Lấy tất cả file trong folder (và thư mục con)
        string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"[SOUNDS LOG] Có {files.Length} file trong thư mục:");
        sb.AppendLine(folderPath);
        sb.AppendLine("======================================");

        foreach (string file in files)
        {
            sb.AppendLine(file);
        }

        // ✅ Chỉ đúng 1 log duy nhất
        Debug.Log(sb.ToString());
    }
}
