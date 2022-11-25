﻿using Markdown;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Markdown
{
    public class TokenParser
    {
        public readonly List<Tag> Tags;

        public TokenParser()
        {
            Tags = new List<Tag>
            {
                new Tag("__", "__", "strong",
                    new [] {"hasSpace", "hasLetter"},
            (tag, token, text, startInd) =>
                            text.TryGetChar(startInd + tag.OpenMark.Length, out var ch1)
                            && !char.IsWhiteSpace(ch1),
            (tag, token, text, endInd) =>
                            text.TryGetChar(endInd - 1, out var ch1)
                            && !char.IsWhiteSpace(ch1), 
           (tag, token, text, endInd) => 
                            token.CustomBools["hasLetter"] &&
                               (!(text.TryGetChar(token.StartOpenMark - 1, out var ch1)
                                  && !char.IsWhiteSpace(ch1)
                                  || text.TryGetChar(endInd + tag.CloseMark.Length, out var ch2)
                                  && !char.IsWhiteSpace(ch2))
                                || !token.CustomBools["hasSpace"]),
        (tag, token, ch) =>
                        {
                            if (char.IsWhiteSpace(ch))
                                token.ChangeCustomBool("hasSpace", true);
                            if (char.IsLetter(ch))
                                token.ChangeCustomBool("hasLetter", true);
                            return false;
                        },
        new List<Func<Token, List<Token>, bool>>
                        {
                            (token, tokens) => token.Parent?.Tag?.HtmlTag != "em",
                        }),

                new Tag("_", "_", "em",
                    new [] {"hasSpace", "hasLetter"},
            (tag, token, text, startInd) =>
                            text.TryGetChar(startInd + tag.OpenMark.Length, out var ch1)
                            && !char.IsWhiteSpace(ch1), 
           (tag, token, text, endInd) =>
                            text.TryGetChar(endInd - 1, out var ch1)
                            && !char.IsWhiteSpace(ch1),
          (tag, token, text, endInd) =>
                            token.CustomBools["hasLetter"] &&
                            (!(text.TryGetChar(token.StartOpenMark - 1, out var ch1)
                               && !char.IsWhiteSpace(ch1)
                               || text.TryGetChar(endInd + tag.CloseMark.Length, out var ch2)
                               && !char.IsWhiteSpace(ch2))
                             || !token.CustomBools["hasSpace"]),
        (tag, token, ch) =>
                        {
                            if (char.IsWhiteSpace(ch))
                                token.ChangeCustomBool("hasSpace", true);
                            if (char.IsLetter(ch))
                                token.ChangeCustomBool("hasLetter", true);
                            return false;
                        },
        new List<Func<Token, List<Token>, bool>>()),

                new Tag("# ", Environment.NewLine, "h1",
                    Array.Empty<string>(),
                    (tag, token, text, startInd) =>
                        {
                            for (int i = startInd; i > 0; i--)
                                if (!char.IsWhiteSpace(text[i]))
                                    return false;
                            return true;
                        },
                    (tag, token, text, endInd) => true,
                    (tag, token, text, endInd) => true,
                    (tag, token, ch) => false,
                    new List<Func<Token, List<Token>, bool>>())
            };
        }

        public List<Token> ParseTokens(string text)
        {
            var tokens = FindTokens(text);
            RemoveUnreadyParents(tokens);
            tokens = RemoveIncorrectInterractedTokens(tokens);
            return tokens
                .OrderBy(t => t.StartOpenMark)
                .ToList();
        }

        private List<Token> RemoveIncorrectInterractedTokens(List<Token> tokens)
        {
            return tokens
                .Where(t =>
                    t.Tag.TagInteractionRules
                        .All(rule => rule(t, tokens)))
                .ToList();
        }

        private List<Token> FindTokens(string text)
        {
            List<Token> tokens = new List<Token>();
            var current = new Token(null!, null!, 0);
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\\')
                {
                    i++;
                    continue;
                }

                if (!IsMarkHere(text, i, out var tag, out var isStart))
                {
                    if (current.ProcessChar(text[i]))
                        current = current.Parent;
                    continue;
                }

                if (!isStart)
                {
                    if (current.TryCloseToken(tag, text, ref i, tokens))
                    {
                        while (current.EndCloseMark != -1)
                            current = current.Parent;
                        continue;
                    }
                }
                current = current.TryOpenChildToken(tag, text, ref i);
            }
            return tokens;
        }

        private void RemoveUnreadyParents(List<Token> tokens)
        {
            foreach (var token in tokens)
            {
                var cur = token;
                while (cur.Parent?.EndCloseMark == -1)
                    cur = cur.Parent;
                token.Parent = cur.Parent;
            }
        }

        private bool IsMarkHere(string text, int index, out Tag tag, out bool isStart)
        {
            foreach (var t in Tags)
            {
                if (text.ContainsItOnIndex(t.CloseMark, index))
                {
                    isStart = false;
                    tag = t;
                    return true;
                }

                if (text.ContainsItOnIndex(t.OpenMark, index))
                {
                    isStart = true;
                    tag = t;
                    return true;
                }
            }

            tag = null!;
            isStart = false;
            return false;
        }
    }
}
