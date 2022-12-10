using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace QOL;
// Credit goes to Grimmdev: https://gist.github.com/grimmdev/979877fcdc943267e44c
// Additionally IJEMIN for: https://gist.github.com/IJEMIN/fdff6db1b1131b91033cbf204247816e

/*
We need this for parsing the JSON, unless you use an alternative.
You will need SimpleJSON if you don't use alternatives.
It can be gotten hither: https://github.com/Bunny83/SimpleJSON
*/
/* Limitations 
translate.googleapis.com is free, but it only allows about 100 requests per one hour.
After that, you will receive 429 error response.
*/

public class Translate : MonoBehaviour
{

    // We use Google's api built into Google Translator.
    public static IEnumerator Process(string targetLang, string sourceText, Action<string> result)
    {
        Debug.Log("Process #1");
        yield return Process("auto", targetLang, sourceText, result);
    }

    // Exactly the same as above but allow the user to change from Auto, for when Google gets wack
    private static IEnumerator Process(string sourceLang, string targetLang, string sourceText,
        Action<string> result)
    {
        Debug.Log("Process #2");
        var url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl="
                  + sourceLang + "&tl=" + targetLang + "&dt=t&q=" + WWW.EscapeURL(sourceText);
        Debug.Log(url);

        using var www = UnityWebRequest.Get(url);
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

public class AuthTranslate : MonoBehaviour
{
    private static readonly string APIKey = Plugin.ConfigAuthKeyForTranslation.Value;

    public static IEnumerator TranslateText(string sourceLanguage, string targetLanguage, string sourceText,
        Action<string> result)
    {
        yield return TranslateTextRoutine(sourceLanguage, targetLanguage, sourceText, result);
    }

    private static IEnumerator TranslateTextRoutine(string sourceLanguage, string targetLanguage, string sourceText,
        Action<string> result)
    {
        var formData = new List<IMultipartFormSection>
        {
            new MultipartFormDataSection("Content-Type", "application/json; charset=utf-8"),
            new MultipartFormDataSection("source", sourceLanguage),
            new MultipartFormDataSection("target", targetLanguage),
            new MultipartFormDataSection("format", "text"),
            new MultipartFormDataSection("q", sourceText)
        };

        var uri = $"https://translation.googleapis.com/language/translate/v2?key={APIKey}";

        using var webRequest = UnityWebRequest.Post(uri, formData);

        yield return webRequest.Send();

        if (webRequest.isError)
        {
            Debug.LogError(webRequest.error);
            Debug.Log("Error occured during AuthTranslation");
        }

        var parsedTexts = JSONNode.Parse(webRequest.downloadHandler.text);
        var translatedText = parsedTexts["data"]["translations"][0]["translatedText"];

        result(translatedText);
    }
}