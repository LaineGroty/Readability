﻿using OpenNLP.Tools.SentenceDetect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Readability
{
    public class TextAnalyzer
    {
        // Constructors -------------------------------------------------------------------------------------
        public TextAnalyzer() { }
        public TextAnalyzer(string text)
            => Text = text;


        // Private helper variables -------------------------------------------------------------------------
        private static readonly EnglishMaximumEntropySentenceDetector SentenceDetector = new EnglishMaximumEntropySentenceDetector(Settings.Default.SentenceModelLocation);
        
        // Dale-Chall list of easy words
        private static readonly string[] EasyWords = Resources.ChallEasyWords.Split('\n').Select(s => s.Trim()).ToArray();
        // Suffixes to try appending to words in easyWords for a more complete list
        private static readonly string[] Suffixes = Resources.WordSuffixes.Split('\n').Select(s => s.Trim()).ToArray();
        // Full english dictionary -- only words found in this dictionary will be considered for difficult words
        private static readonly string[] RealWords = File.ReadAllLines("./Resources/words.txt");
        
        // Public variables ---------------------------------------------------------------------------------
        public string Text { get; set; }

        public int Words { get => WordsInText.Length; }
        public string[] WordsInText 
        { get => Regex.Matches(Text, @"(?:\b)\w+(?:\b)").Cast<Match>().Select(m => m.Value).ToArray(); }
        public int Sentences 
        { get => SentenceDetector.SentenceDetect(Text).Length; }
        public double FleschKincaidGrade 
        {
            get
            {
                Debug.WriteLine($"{Words} {Sentences} {WordsInText.Sum(w => SyllableCount(w))} {WordsInText.Count(w => SyllableCount(w) >= 3)}");
                return 0.39 * Words / Sentences + 11.8 * WordsInText.Sum(w => SyllableCount(w)) / Words - 15.59;
            }
        }
        // TODO: Optimize, write docs
        public double DaleChallScore
        {
            get
            {
                int difficultWords = WordsInText.Where(w => RealWords.Contains(w)).Count(s =>
                {
                    s = s.ToLower();
                    foreach(string suffix in Suffixes)
                        foreach(string w in EasyWords)
                            if(s.Equals(w + suffix))
                                return false;
                    return true;
                });

                double score = 0.1579 * difficultWords / Words * 100 + 0.0496 * Words / Sentences;
                if((double)difficultWords / Words > 0.05)
                    score += 3.6365;
                return score;
            }
        }
        // TODO: Verify implementation
        public double DaleChallGrade 
        {
            get
            {
                for(int i = 5; i <= 10; ++i)
                    if(DaleChallScore <= i + 0.5)
                        return (i - 5) * 2 + i;
                    else if(DaleChallScore < i + 1)
                        return (i - 5) * 2 + i + 1;
                return 4;
            }
        }
        public double GunningFog 
        { get => 0.4 * (Words / Sentences + 100 * WordsInText.Count(w => SyllableCount(w) >= 3) / Words); }

        // Helper methods -----------------------------------------------------------------------------------
        // TODO: Improve accuracy
        private int SyllableCount(string word)
        {
            // https://codereview.stackexchange.com/questions/9972/syllable-counting-function
            word = word.ToLower().Trim();
            bool lastWasVowel = false;
            char[] vowels = new char[] { 'a', 'e', 'i', 'o', 'u', 'y' };
            int count = 0;

            foreach(char c in word)
                if(vowels.Contains(c))
                {
                    if(!lastWasVowel)
                        count++;
                    lastWasVowel = true;
                }
                else
                    lastWasVowel = false;

            if((word.EndsWith("e") || (word.EndsWith("es") || word.EndsWith("ed")))
                  && !word.EndsWith("le"))
                count--;

            return count;
        }
    }
}
