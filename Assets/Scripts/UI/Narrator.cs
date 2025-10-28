using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace UI
{
    public class Narrator : MonoBehaviour
    {
        public enum Level
        {
            Info,
            Warning
        }
        
        public static Narrator GetInstance()
        {
            return GameObject.FindGameObjectWithTag("Narrator").GetComponent<Narrator>();
        }

        [Serializable]
        public struct SayLevelOption
        {
            public Level level;
            public Color color;
        }
        
        [Header("References")]
        [SerializeField] TextMeshProUGUI subtitles;

        [Header("GameSettingsPreset")] 
        [SerializeField, Min(0)] private float fadeTime;
        [SerializeField, Min(0)] private float openTimeScalar;
        [SerializeField] private List<SayLevelOption> sayLevels = new ();
        
        private Color textColor;
        private float maxAlpha;
        
        private void Awake()
        {
            SetColor(Level.Info);
        }

        private void SetColor(Level level)
        {
            textColor = sayLevels.First(s => s.level == level).color;
            subtitles.faceColor = textColor;
            maxAlpha = textColor.a;
        }

        private void Start()
        {
            // StartCoroutine(Test());
            textColor.a = 0;
            subtitles.faceColor = textColor;
        }

        #if UNITY_EDITOR
        private IEnumerator Test()
        {
            yield return StartCoroutine(SayLots(new List<string>() { "among is my favourite game evrrrrr", "its so good" }));
            print("done saying");
        }
        #endif

        public IEnumerator SayLots(List<string> sentences)
        {
            foreach (string sentence in sentences)
            {
                yield return StartCoroutine(Say(sentence, Level.Info));
            }
        }

        public IEnumerator Say(string text, Level level)
        {
            SetColor(level);
            
            subtitles.text = text;
            yield return StartCoroutine(DoSay());
        }

        IEnumerator Fade(float normalizedStart, float normalizedEnd)
        {
            float t = 0;

            while(t < fadeTime) {
                t += Time.deltaTime;
                
                float blend = Mathf.Clamp01(t / fadeTime);
                textColor.a = Mathf.Lerp(normalizedStart, normalizedEnd, blend);

                subtitles.faceColor = textColor;

                yield return null;
            }
        }

        IEnumerator DoSay()
        {
            yield return StartCoroutine(Fade(0, maxAlpha));
            yield return new WaitForSeconds(openTimeScalar * subtitles.text.Length);
            yield return StartCoroutine(Fade(maxAlpha, 0));
        }
    }
}