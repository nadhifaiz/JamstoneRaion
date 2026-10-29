using System.Collections;
using UnityEngine;

// Kontrak pemutar cutscene. CutsceneManager hanya mengenal kelas ini.
// Untuk cutscene sungguhan nanti, buat kelas turunan baru (slideshow, video, Timeline, dll.)
// dan ganti komponen pemutar di scene. Pemicu, save, dan alur game tidak perlu diubah.
public abstract class CutscenePlayerBase : MonoBehaviour
{
    public abstract IEnumerator Play(CutsceneSO cutscene);
}