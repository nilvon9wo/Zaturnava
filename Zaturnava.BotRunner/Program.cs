using Net.NowhereAtAll.Zaturnava.BotRunner;

// Only the operating system starting this program runs this file, so it is not measured
// (ZaturnavaUnmeasuredFile in Zaturnava.BotRunner.Test); the command it runs is.
return await SelfPlayCommand.Run(args, Console.Out, Console.Error, CancellationToken.None);
