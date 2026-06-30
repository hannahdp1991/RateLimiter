import torch
import torch.nn as nn
import torch.optim as optim
import torchvision
import torchvision.transforms as transforms
import matplotlib.pyplot as plt

# 1. Load dataset (MNIST digits)
transform = transforms.Compose([transforms.ToTensor()])

train_data = torchvision.datasets.MNIST(
    root='./data',
    train=True,
    download=True,
    transform=transform
)

train_loader = torch.utils.data.DataLoader(
    train_data,
    batch_size=64,
    shuffle=True
)

# 2. Define neural network
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

model = NeuralNet()

# 3. Loss and optimizer
criterion = nn.CrossEntropyLoss()
optimizer = optim.Adam(model.parameters(), lr=0.001)

# 4. Training loop
epochs = 3

for epoch in range(epochs):
    for images, labels in train_loader:
        outputs = model(images)
        loss = criterion(outputs, labels)

        optimizer.zero_grad()
        loss.backward()
        optimizer.step()

    print(f"Epoch {epoch+1}, Loss: {loss.item():.4f}")
    
torch.save(model.state_dict(), "model.pth")
print("Training complete!")

# Get test dataset
test_data = torchvision.datasets.MNIST(
    root='./data',
    train=False,
    download=True,
    transform=transform
)

# Pick one image
image, label = test_data[60]

# Show image
plt.imshow(image.squeeze(), cmap='gray')
plt.title(f"Actual label: {label}")
plt.show()

# Predict
image = image.unsqueeze(0)  # add batch dimension
output = model(image)

# Get predicted number
predicted = torch.argmax(output)

print("Model prediction:", predicted.item())
print("Actual number:", label)