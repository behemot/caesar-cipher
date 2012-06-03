/**
 * Caesar cipher http://en.wikipedia.org/wiki/Caesar_cipher
 *
 * Author: Michał Białas <michal.bialas@mbialas.pl>
 * Since: 2011-11-13
 * Version: 0.1
 */

using System;
using System.IO;
using System.Text;
using System.Numerics;
using System.Collections.Generic;
using NDesk.Options;

public class Modulo
{	
	public static void Main(string[] args)
	{
		bool showHelp = false;
		string key = null, inputPath = null, outputPath = null, command;

		OptionSet opt = new OptionSet() {
			{"k|key=", "in decrypt is modulo key, generated in encryption", v => key = v},
			{"i|in=", "input file", v => inputPath = v},
			{"o|out=", "output file", v => outputPath = v},
			{"h|help", "show this message and exit", v => showHelp = v != null},
		};

		List<string> commands;
		try {
			commands = opt.Parse(args);
		} catch (OptionException e) {
			PrintError(e.Message);
			return;
		}

		if (showHelp) {
            Usage(opt);
            return;
        }

		if (0 == commands.Count) {
			PrintError("There is no command.");
			return;
		}
		
		command = commands[0];
		
		if ("encrypt" != command && "decrypt" != command) {
			PrintError("Invalid command.");
			return;
		}
		
		if ("decrypt" == command && null == key) {
			PrintError("There is no key to decrypt message.");
			return;
		}

		if (null == inputPath) {
			PrintError("The input file is not specified");
			return;
		}

		if (null == outputPath) {
			PrintError("The output file is not specified");
			return;
		}

		FileStream inputFile, outputFile;
		try {
			inputFile = File.Open(inputPath, FileMode.Open, FileAccess.Read, FileShare.None);
			outputFile = File.Open(outputPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
		} catch (Exception e) {
			PrintError(e.Message);
			return;
		}
		
		Crypt crypt = new Crypt();
		crypt.inputFile = inputFile;
		crypt.outputFile = outputFile;
		string outputText = "";
		switch (command) {
			case "encrypt":
				outputText = crypt.Encrypt();
				break;
			case "decrypt":
				crypt.key = key;
				outputText = crypt.Decrypt();
				break;
		}

		Console.WriteLine(outputText);
		inputFile.Close();
		outputFile.Close();
	}
	
	public static void Usage(OptionSet opt)
	{
		Console.WriteLine("Usage: Modulo.exe (encrypt|decrypt) [/key:decrypt_key] /in:inupt_file /out:output_file");
        Console.WriteLine("Options:");
        opt.WriteOptionDescriptions(Console.Out);
	}
	
	public static void PrintError(string message)
	{
		Console.WriteLine("Error: {0}", message);
		Console.WriteLine ("Try Modulo.exe /help for more information.");
	}
}

class Crypt
{
	public const int MAX_CHARS = 256;
	public const int p = 10;
	
	public FileStream inputFile { get; set; }
	 
	public FileStream outputFile { get; set; }
	 
	public string key { get; set; }
	 
	public string Decrypt()
	{
		decimal a, b, maxValue;
		maxValue = Util.DecimalPower(MAX_CHARS, p);
		
		string[] keys = Encoding.UTF8.GetString(Convert.FromBase64String(key)).Split(new Char [] {':'});
		a = Convert.ToDecimal(keys[0]);
		b = Convert.ToDecimal(keys[1]);
		decimal inva = Util.GetInverse(a, maxValue);
		
		int n; decimal k;
		byte[] buffer = new byte[p];
		BigInteger sub, inv, maxVal = new BigInteger(maxValue);
		while ((n = inputFile.Read(buffer, 0, buffer.Length)) > 0) {
			k = Util.ToDecimal(buffer, n);
			sub = new BigInteger(k - b);
			sub = Util.Normalize(sub, maxVal);
			inv = new BigInteger(inva);
			inv = Util.Normalize(inv, maxVal);
			k = (decimal)(BigInteger.Remainder(sub * inv, maxVal));
			buffer = Util.ToBytes(k);
			outputFile.Write(buffer, 0, n);
		}

		return "Done";
	}
	 
	public string Encrypt()
	{
		decimal a, b, maxValue;
		maxValue = Util.DecimalPower(MAX_CHARS, p);
		b = Util.GenerateNumber(maxValue);
		do {
			a = Util.GenerateNumber(maxValue);
		} while (1m != Util.Nwd(a, maxValue));
		
		int n; decimal k;
		byte[] buffer = new byte[p];
		BigInteger ba, bb, bk, maxVal = new BigInteger(maxValue);
		while ((n = inputFile.Read(buffer, 0, buffer.Length)) > 0) {
			k = Util.ToDecimal(buffer, n);
			ba = new BigInteger(a);
			bb = new BigInteger(b);
			bk = new BigInteger(k);
			k = (decimal)(BigInteger.Remainder(ba * bk + bb, maxVal));
			buffer = Util.ToBytes(k);
			outputFile.Write(buffer, 0, n);
		}
		
		byte[] key = Encoding.UTF8.GetBytes(a.ToString() + ":" + b.ToString());
		string message = "Done\nkey: " + Convert.ToBase64String(key);
		return message;
	}
}

class Util
{
	private const int MAX_DECIMAL = 9;

	public static decimal Nwd(decimal a, decimal b)
	{
		decimal c = 0m;
		while (0m != b) {
			c = a % b;
			a = b;
			b = c;
		}
		return a;
	}
	
	public static decimal GenerateNumber(decimal maximum)
	{
		int digits = maximum.ToString().Length + 1;
		StringBuilder tmp = new StringBuilder();
		Random rand = new Random();
		for (int i = 0; i < digits; i += 1) {
			tmp.Append(rand.Next(MAX_DECIMAL + 1).ToString());
		}
		return Convert.ToDecimal(tmp.ToString()) % (maximum + 1m);
	}
	
	public static decimal DecimalPower(int x, int y)
	{
		decimal prod = 1;
		for (int i = 0; i < y; i += 1) {
			prod *= x;
		}
		return prod;
	}
	
	public static decimal ToDecimal(byte[] input, int n)
	{
		byte[] buffer = new byte[10];
		for (int i = 0; i < n && i < 10; i += 1) {
			buffer[i] = input[i];
		}
		
		decimal result = 0m;
		for (int i = 0; i < n && i < 10; i += 1) {
			result += buffer[i] * DecimalPower(256, i);
		}

		return result;
	}
	
	public static byte[] ToBytes(decimal input)
	{
		byte[] result = new byte[10];
		
		for (int i = 0; i < 10; i += 1) {
			result[i] = (byte)(input % 256);
			input = Math.Floor(input / 256);
		}

		return result;
	}
	
	public static decimal GetInverse(decimal a, decimal b)
	{
		decimal p = 1m, q = 0m, r = 0m, s = 1m, quotient, tmp;

		while (0m != b) {
			tmp = a % b;
			quotient = Math.Floor(a / b);
			a = b;
			b = tmp;
			tmp = p - quotient * r;
			p = r;
			r = tmp;
			tmp = q - quotient * s;
			q = s;
			s = tmp;
		}

		return p;
	}
	
	public static BigInteger Normalize(BigInteger a, BigInteger maxVal)
	{
		while (a < 0) {
			a += maxVal;
		}
		return a;
	}
}

