﻿// Copyright (c) Richasy. All rights reserved.

using System;
using System.Linq;
using System.Text.RegularExpressions;
using Bilibili.App.Dynamic.V2;
using Bilibili.Main.Community.Reply.V1;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media.Imaging;

namespace Richasy.Bili.App.Controls
{
    /// <summary>
    /// 带表情的文本.
    /// </summary>
    public sealed class EmotiTextBlock : Control
    {
        /// <summary>
        /// <see cref="MaxLines"/> 的依赖属性.
        /// </summary>
        public static readonly DependencyProperty MaxLinesProperty =
            DependencyProperty.Register(nameof(MaxLines), typeof(int), typeof(EmotiTextBlock), new PropertyMetadata(4));

        /// <summary>
        /// <see cref="ReplyInfo"/> 的依赖属性.
        /// </summary>
        public static readonly DependencyProperty ReplyInfoProperty =
            DependencyProperty.Register(nameof(ReplyInfo), typeof(ReplyInfo), typeof(EmotiTextBlock), new PropertyMetadata(null, new PropertyChangedCallback(OnReplyInfoChanged)));

        /// <summary>
        /// <see cref="DynamicDescription"/> 的依赖属性.
        /// </summary>
        public static readonly DependencyProperty DynamicDescriptionProperty =
            DependencyProperty.Register(nameof(DynamicDescription), typeof(ModuleDesc), typeof(EmotiTextBlock), new PropertyMetadata(null, new PropertyChangedCallback(OnDynamicDescriptionChanged)));

        private RichTextBlock _richBlock;
        private RichTextBlock _flyoutRichBlock;
        private Button _overflowButton;
        private bool _isOverflowInitialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmotiTextBlock"/> class.
        /// </summary>
        public EmotiTextBlock() => DefaultStyleKey = typeof(EmotiTextBlock);

        /// <summary>
        /// 最大行数.
        /// </summary>
        public int MaxLines
        {
            get { return (int)GetValue(MaxLinesProperty); }
            set { SetValue(MaxLinesProperty, value); }
        }

        /// <summary>
        /// 回复信息.
        /// </summary>
        public ReplyInfo ReplyInfo
        {
            get { return (ReplyInfo)GetValue(ReplyInfoProperty); }
            set { SetValue(ReplyInfoProperty, value); }
        }

        /// <summary>
        /// 动态描述信息.
        /// </summary>
        public ModuleDesc DynamicDescription
        {
            get { return (ModuleDesc)GetValue(DynamicDescriptionProperty); }
            set { SetValue(DynamicDescriptionProperty, value); }
        }

        /// <summary>
        /// 重置状态.
        /// </summary>
        public void Reset() => _richBlock?.Blocks?.Clear();

        /// <inheritdoc/>
        protected override void OnApplyTemplate()
        {
            _richBlock = GetTemplateChild("RichBlock") as RichTextBlock;
            _flyoutRichBlock = GetTemplateChild("FlyoutRichBlock") as RichTextBlock;
            _overflowButton = GetTemplateChild("OverflowButton") as Button;

            _richBlock.IsTextTrimmedChanged += OnIsTextTrimmedChanged;
            _overflowButton.Click += OnOverflowButtonClick;

            InitializeContent();
            base.OnApplyTemplate();
        }

        private static void OnReplyInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as EmotiTextBlock;
            if (e.NewValue != null)
            {
                instance.DynamicDescription = null;
                instance.InitializeContent();
            }
        }

        private static void OnDynamicDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as EmotiTextBlock;
            if (e.NewValue != null)
            {
                instance.ReplyInfo = null;
                instance.InitializeContent();
            }
        }

        private void OnIsTextTrimmedChanged(RichTextBlock sender, IsTextTrimmedChangedEventArgs args)
            => _overflowButton.Visibility = sender.IsTextTrimmed ? Visibility.Visible : Visibility.Collapsed;

        private void InitializeContent()
        {
            _isOverflowInitialized = false;
            if (_richBlock != null)
            {
                _richBlock.Blocks.Clear();
                Paragraph para = null;
                if (ReplyInfo != null)
                {
                    para = ParseReplyInfo();
                }
                else if (DynamicDescription != null)
                {
                    para = ParseDynamicDescription();
                }

                if (para != null)
                {
                    _richBlock.Blocks.Add(para);
                }
            }

            if (_overflowButton != null && _richBlock != null)
            {
                _overflowButton.Visibility = _richBlock.IsTextTrimmed ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void OnOverflowButtonClick(object sender, RoutedEventArgs e)
        {
            if (!_isOverflowInitialized)
            {
                _flyoutRichBlock.Blocks.Clear();

                if (ReplyInfo != null)
                {
                    var para = ParseReplyInfo();
                    _flyoutRichBlock.Blocks.Add(para);
                }
                else if (DynamicDescription != null)
                {
                    var para = ParseDynamicDescription();
                    _flyoutRichBlock.Blocks.Add(para);
                }
            }
        }

        private Paragraph ParseReplyInfo()
        {
            var message = ReplyInfo.Content.Message;
            var para = new Paragraph();

            if (ReplyInfo.Content.Emote != null && ReplyInfo.Content.Emote.Count > 0)
            {
                // 有表情存在，进行处理.
                var emotiRegex = new Regex(@"(\[.*?\])");
                var emoties = ReplyInfo.Content.Emote;
                var splitCotents = emotiRegex.Split(message).Where(p => p.Length > 0).ToArray();
                foreach (var content in splitCotents)
                {
                    if (emotiRegex.IsMatch(content))
                    {
                        if (emoties.TryGetValue(content, out var sourceEmoti))
                        {
                            var inlineCon = new InlineUIContainer();
                            var img = new Image() { Width = 20, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 2, -4) };
                            var bitmap = new BitmapImage(new Uri(sourceEmoti.Url)) { DecodePixelWidth = 40 };
                            img.Source = bitmap;
                            inlineCon.Child = img;
                            para.Inlines.Add(inlineCon);
                        }
                        else
                        {
                            para.Inlines.Add(new Run { Text = content });
                        }
                    }
                    else
                    {
                        para.Inlines.Add(new Run { Text = content });
                    }
                }
            }
            else
            {
                para.Inlines.Add(new Run { Text = message });
            }

            return para;
        }

        private Paragraph ParseDynamicDescription()
        {
            var text = DynamicDescription.Text;
            var descs = DynamicDescription.Desc;
            var para = new Paragraph();

            if (descs.Count > 0 && descs.Any(p => p.Type == DescType.Emoji))
            {
                // 有表情存在，进行处理.
                var emotiRegex = new Regex(@"(\[.*?\])");
                var emoties = descs.Where(p => p.Type == DescType.Emoji);
                var splitCotents = emotiRegex.Split(text).Where(p => p.Length > 0).ToArray();
                foreach (var content in splitCotents)
                {
                    if (emotiRegex.IsMatch(content))
                    {
                        var emoji = emoties.FirstOrDefault(p => p.Text.Equals(content));
                        if (emoji != null)
                        {
                            var inlineCon = new InlineUIContainer();
                            var img = new Image() { Width = 20, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 2, -4) };
                            var bitmap = new BitmapImage(new Uri(emoji.Uri)) { DecodePixelWidth = 40 };
                            img.Source = bitmap;
                            inlineCon.Child = img;
                            para.Inlines.Add(inlineCon);
                        }
                        else
                        {
                            para.Inlines.Add(new Run { Text = content });
                        }
                    }
                    else
                    {
                        para.Inlines.Add(new Run { Text = content });
                    }
                }
            }
            else
            {
                para.Inlines.Add(new Run { Text = text });
            }

            return para;
        }
    }
}
