string inputFilePath = "C:\\Users\\User\\Documents\\MIDE\\Dev\\MiToMo\\gesture-pad.csv"; // Input CSV file path

// Read all lines from the CSV file
string[] lines = File.ReadAllLines(inputFilePath);

// Iterate through each line and count the number of numbers
for (int i = 0; i < lines.Length; i++)
{
    string line = lines[i];
    int numberOfElements = line.Split(',').Length;

    // Output the line number and count of elements
    Console.WriteLine($"Line {i + 1}: {numberOfElements} numbers");
}
