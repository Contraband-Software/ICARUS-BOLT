using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    public class MainMenuUI : MonoBehaviour
    {



        public void Button_Play()
        {
            SceneManager.LoadScene("VSlice");
        }
    }
}
