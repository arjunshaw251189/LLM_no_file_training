Lightweight JSON Q&A Engine (C#)

A simple and cost-efficient C# tool to query and extract answers from local JSON files using system prompt engineering.
No heavy GPUs, no expensive infrastructure — just drop in your .json files, connect to any LLM API, and start asking natural language questions.


---

✨ Features

💸 Low-cost operation – Minimal hardware requirements, perfect for budget-friendly projects.

📂 Local data processing – Reads JSON files directly from the dummydata folder.

🤖 LLM-ready – Works with any basic Large Language Model API.

⚡ Quick setup – Just add your files, set credentials, and you’re ready.



---

🚀 Getting Started

1️⃣ Place Your Files

Add all .json files into the dummydata folder.
Example file included: dummydata/6205.json

2️⃣ Configure Credentials

Open the config file and add your API Key and Token for the LLM you want to use.

3️⃣ Run the Project

Start the application and ask questions in natural language.


---

📌 Example JSON

dummydata/6205.json

{
  "taxonomy": "Bearings/Ball bearings/Deep groove ball bearings",
  "designation": "6205",
  "short_description": "Deep groove ball bearing",
  "dimensions": [
    { "name": "Outside diameter", "value": 52, "unit": "mm" },
    { "name": "Bore diameter", "value": 25, "unit": "mm" },
    { "name": "Width", "value": 15, "unit": "mm" }
  ]
}


---

💻 Example Usage

Question:

what is the dimension of 6205

Answer:

[
  {
    "answer": [
      "Outside diameter is 52 mm.",
      "Bore diameter is 25mm."
    ],
    "subject": "6205",
    "attribute": "diameter",
    "comparative": false,
    "detectedlanguage": "English"
  }
]

Question:

diameter of 6205 and 6205 n

Answer:

[
  {
    "answer": [
      "Outside diameter is 52mm.",
      "Bore diameter is 25mm."
    ],
    "subject": "6205",
    "attribute": "diameter",
    "comparative": false,
    "detectedlanguage": "English"
  },
  {
    "answer": [
      "Outside diameter is 52mm.",
      "Bore diameter is 25mm."
    ],
    "subject": "6205 n",
    "attribute": "diameter",
    "comparative": false,
    "detectedlanguage": "English"
  },
  {
    "answer": [
      "No difference found."
    ],
    "subject": "6205,6205 n",
    "attribute": "diameter,diameter",
    "comparative": true,
    "detectedlanguage": null
  }
]


---

🛠 Notes

Designed for low GPU usage and minimal running cost.

Supports translation and on-demand calculations.

Works with any LLM endpoint — just replace API details.

Suitable for offline or low-budget scenarios.



---

📄 License

MIT License – feel free to use, modify, and distribute.


---

