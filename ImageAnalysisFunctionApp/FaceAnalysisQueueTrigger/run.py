import os
import json
import httplib, urllib, base64

headers = {
    # Request headers
    'Content-Type': 'application/json',
    'Ocp-Apim-Subscription-Key': 'e33e56b2ce424146b4bedf339105dd99',
}

# read the queue message and write to stdout
URLimage = open(os.environ['input']).read()
message = "Processing image '{0}'".format(URLimage)
print(message)

params = urllib.urlencode({
    # Request parameters
    'returnFaceId': 'true',
    'returnFaceLandmarks': 'false',
    'returnFaceAttributes': "age,gender,emotion",
})

body = {
    "url": str(URLimage)
}


conn = httplib.HTTPSConnection('westus.api.cognitive.microsoft.com')
conn.request("POST", "/face/v1.0/detect?%s" % params, str(body), headers)
response = conn.getresponse()
data = response.read()
print(data)
conn.close()

jdata = json.loads(data)
for face in jdata:
    fid = face['faceId'] 
    age = face['faceAttributes']['age']
    sex = face['faceAttributes']['gender']

    emotions = face['faceAttributes']['emotion'].items()
    top_emo = sorted(emotions, key=lambda x: x[1])[-1][0]
    print("Detected {0} year old {1} feeling {2}: {3}".format(age, sex, top_emo, fid)) 