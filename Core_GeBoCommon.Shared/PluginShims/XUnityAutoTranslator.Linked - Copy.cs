#if AUTOTRANSLATOR
using GeBoCommon.PluginShims;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeBoCommon.PluginShims
{
	public partial class XUnityAutoTranslator
	{
		private void Initialize()
		{
			_defaultTranslator = XUnity.AutoTranslator.Plugin.Core.AutoTranslator.Default;
			_tryTranslate = XUnity.AutoTranslator.Plugin.Core.AutoTranslator.Default.TryTranslate;
			_translateAsyncWrapper = (TranslateAsyncDelegate)((untranslatedText, onCompleted) =>
			{
				XUnity.AutoTranslator.Plugin.Core.AutoTranslator.Default.TranslateAsync(untranslatedText, (result) => onCompleted(new TranslationResultWrapper(result)));
			});
		}

		public partial class TranslationResultWrapper
		{
			private XUnity.AutoTranslator.Plugin.Core.TranslationResult ResultSource = null;
			private void Initialize() => ResultSource = Source as XUnity.AutoTranslator.Plugin.Core.TranslationResult;
			public bool Succeeded => ResultSource?.Succeeded ?? false;
			public string TranslatedText => ResultSource?.TranslatedText;
			public string ErrorMessage => ResultSource?.ErrorMessage;
		}
	}
}
#endif