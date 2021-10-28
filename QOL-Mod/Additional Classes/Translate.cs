using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;


namespace QOL
{
    // Credit goes to Grimmdev: https://gist.github.com/grimmdev/979877fcdc943267e44c

    // We need this for parsing the JSON, unless you use an alternative.
    // You will need SimpleJSON if you don't use alternatives.
    // It can be gotten hither: https://github.com/Bunny83/SimpleJSON

    public class Translate
    {
        // We have used Google's own api built into google Translator.
        public static IEnumerator Process(string targetLang, string sourceText, System.Action<string> result)
        {
            yield return Process("auto", targetLang, sourceText, result);
        }

        // Exactly the same as above but allow the user to change from Auto, for when google gets all wacky
        public static IEnumerator Process(string sourceLang, string targetLang, string sourceText, System.Action<string> result)
        {
            string url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl="
                         + sourceLang + "&tl=" + targetLang + "&dt=t&q=" + WWW.EscapeURL(sourceText);

            WWW www = new WWW(url);
            yield return www;

            if (www.isDone)
            {
                if (string.IsNullOrEmpty(www.error))
                {
                    var N = JSONNode.Parse(www.text);
                    string translatedText = N[0][0][0];

                    result(translatedText);
                }
            }
        }
    }





































































}
