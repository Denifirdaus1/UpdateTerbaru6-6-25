# PythonEngine/api_client.py
import requests

def call_gemini(prompt):
    url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent"
    headers = {"Content-Type": "application/json"}
    payload = {
        "contents": [{"parts": [{"text": prompt}]}]
    }

    response = requests.post(f"{url}?key=AIzaSyAFyJUMmYXvQxgmGLcL69Gf3GtmeSZMHiY",
                             headers=headers, json=payload)

    try:
        return response.json()['candidates'][0]['content']['parts'][0]['text']
    except Exception as e:
        print(f"Error calling Gemini API: {e}")
        print(f"Response: {response.text}")
        return "Terjadi kesalahan saat menghubungi Gemini API."

