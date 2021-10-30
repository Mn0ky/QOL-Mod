using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;
    

namespace QOL
{
    // Credit goes to Grimmdev: https://gist.github.com/grimmdev/979877fcdc943267e44c

    /*
    We need this for parsing the JSON, unless you use an alternative.
    You will need SimpleJSON if you don't use alternatives.
    It can be gotten hither: https://github.com/Bunny83/SimpleJSON
    */

    /* Limitations 
    // translate.googleapis.com is free, but it only allows about 100 requests per one hour.
    // After that, you will receive 429 error response.
    */

    public class Translate : MonoBehaviour
    {

        // We use Google's api built into Google Translator.
        public static IEnumerator Process(string targetLang, string sourceText, System.Action<string> result)
        {
            Debug.Log("Process #1");
            yield return Process("auto", targetLang, sourceText, result);
        }

        // Exactly the same as above but allow the user to change from Auto, for when Google gets wack
        public static IEnumerator Process(string sourceLang, string targetLang, string sourceText, System.Action<string> result)
        {
            Debug.Log("Process #2");
            string url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl="
                         + sourceLang + "&tl=" + targetLang + "&dt=t&q=" + WWW.EscapeURL(sourceText);
            Debug.Log(url);

            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                // www.SetRequestHeader("user-agent", "placeholder");
                yield return www.Send();

                Debug.Log("Got response! (translate)");

                if (www.isDone)
                {
                    if (!www.isError)
                    {
                        Debug.Log(www.downloadHandler.text);
                        Debug.Log("Parsing Json (translate)");
                        var N = JSONNode.Parse(www.downloadHandler.text);
                        Debug.Log("Json parsed : " + N);
                        string translatedText = N[0][0][0];
                        Debug.Log("translatedText: " + translatedText);
                        result(translatedText);
                    }
                    else
                    {
                        Debug.Log("An error occurred during translation; perhaps too many requests");
                    }
                }
            }

        }
    }
}
