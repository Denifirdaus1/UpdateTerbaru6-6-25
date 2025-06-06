# PythonEngine/api_client.py
import requests

def call_gemini(prompt):
    url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent"
    headers = {"Content-Type": "application/json"}
    payload = {
        "contents": [{"parts": [{"text": prompt}]}]
    }

    response = requests.post(f"{url}?key=AIzaSyDBwcbcNsCxw2zN4JFm312N-CLikeVdC80",
                             headers=headers, json=payload)

    try:
        return response.json()['candidates'][0]['content']['parts'][0]['text']
    except:
        return "Terjadi kesalahan saat menghubungi Gemini API."