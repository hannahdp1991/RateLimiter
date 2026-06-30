import tkinter as tk
import torch
import torch.nn as nn
from PIL import Image, ImageDraw, ImageOps
import numpy as np

# ----- same model structure as training -----
class NeuralNet(nn.Module):
    def __init__(self):
        super(NeuralNet, self).__init__()
        self.model = nn.Sequential(
            nn.Flatten(),
            nn.Linear(28*28, 128),
            nn.ReLU(),
            nn.Linear(128, 10)
        )

    def forward(self, x):
        return self.model(x)

# Load trained model
model = NeuralNet()
model.load_state_dict(torch.load("model.pth"))
model.eval()

# ----- GUI setup -----
window = tk.Tk()
window.title("Draw Digit - AI Predictor")

canvas = tk.Canvas(window, width=200, height=200, bg="white")
canvas.pack()

img = Image.new("L", (200, 200), color=255)
draw = ImageDraw.Draw(img)

# Draw function
def paint(event):
    x, y = event.x, event.y
    r = 8
    canvas.create_oval(x-r, y-r, x+r, y+r, fill="black")
    draw.ellipse([x-r, y-r, x+r, y+r], fill=0)

canvas.bind("<B1-Motion>", paint)

# Predict function
def predict():
    global img

    # Resize to MNIST format
    img_resized = img.resize((28, 28))
    img_resized = ImageOps.invert(img_resized)

    data = np.array(img_resized) / 255.0
    tensor = torch.tensor(data, dtype=torch.float32).unsqueeze(0)

    output = model(tensor)
    prediction = torch.argmax(output)

    result_label.config(text=f"Prediction: {prediction.item()}")

# Clear canvas
def clear():
    canvas.delete("all")
    global img
    img = Image.new("L", (200, 200), color=255)
    draw = ImageDraw.Draw(img)

# Buttons
btn_predict = tk.Button(window, text="Predict", command=predict)
btn_predict.pack()

btn_clear = tk.Button(window, text="Clear", command=clear)
btn_clear.pack()

result_label = tk.Label(window, text="Draw a digit")
result_label.pack()

window.mainloop()