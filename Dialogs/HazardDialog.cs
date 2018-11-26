// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BasicBot.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// Demonstrates the following concepts:
    /// - Use a subclass of ComponentDialog to implement a multi-turn conversation
    /// - Use a Waterflow dialog to model multi-turn conversation flow
    /// - Use custom prompts to validate user input
    /// - Store conversation and user state.
    /// </summary>
    public class HazardDialog : ComponentDialog
    {
        // User state for greeting dialog
        private const string GreetingStateProperty = "greetingState";
        private const string NameValue = "greetingName";
        private const string CityValue = "greetingCity";

        // Prompts names
        private const string IdPrompt = "idPrompt";
        private const string CityPrompt = "cityPrompt";
        private const string DescriptionPrompt = "descriptionPrompt";

        // Minimum length requirements for city and name
        private const int NameLengthMinValue = 3;
        private const int CityLengthMinValue = 5;

        // Dialog IDs
        private const string ProfileDialog = "profileDialog";

        /// <summary>
        /// Initializes a new instance of the <see cref="HazardDialog"/> class.
        /// </summary>
        /// <param name="botServices">Connected services used in processing.</param>
        /// <param name="botState">The <see cref="UserState"/> for storing properties at user-scope.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> that enables logging and tracing.</param>
        public HazardDialog(IStatePropertyAccessor<HazardState> hazardProfileStateAccessor, ILoggerFactory loggerFactory)
            : base(nameof(HazardDialog))
        {
            HazardProfilerAccessor = hazardProfileStateAccessor ?? throw new ArgumentNullException(nameof(hazardProfileStateAccessor));

            // Add control flow dialogs
            var waterfallSteps = new WaterfallStep[]
            {
                    InitializeStateStepAsync,
                    PromptForIdStepAsync,
                    PromptForProblemStepAsync,
                    PromptForDescriptionStepAsync,
                    DisplayGreetingStateStepAsync,
            };
            AddDialog(new WaterfallDialog(ProfileDialog, waterfallSteps));
            AddDialog(new TextPrompt(IdPrompt, ValidateId));
            AddDialog(new ChoicePrompt(CityPrompt) { Style = ListStyle.SuggestedAction });
            AddDialog(new TextPrompt(DescriptionPrompt, ValidateDescription));
        }

        public IStatePropertyAccessor<HazardState> HazardProfilerAccessor { get; }

        private async Task<DialogTurnResult> InitializeStateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var hazardState = await HazardProfilerAccessor.GetAsync(stepContext.Context, () => null);

            hazardState = null;

            if (hazardState == null)
            {
                var hazardStateOpt = stepContext.Options as HazardState;
                if (hazardStateOpt != null)
                {
                    await HazardProfilerAccessor.SetAsync(stepContext.Context, hazardStateOpt);
                }
                else
                {
                    await HazardProfilerAccessor.SetAsync(stepContext.Context, new HazardState());
                }
            }

            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> PromptForIdStepAsync(
                                                WaterfallStepContext stepContext,
                                                CancellationToken cancellationToken)
        {
            var greetingState = await HazardProfilerAccessor.GetAsync(stepContext.Context);

            if (string.IsNullOrWhiteSpace(greetingState.Id))
            {
                // prompt for name, if missing
                var opts = new PromptOptions
                {
                    Prompt = new Activity
                    {
                        Type = ActivityTypes.Message,
                        Text = "שלום, וברוכים הבאים לבוט דיווח מפגעים של עיריית תל אביב יפו, אנא הזן תעודת זהות",
                    },
                };
                return await stepContext.PromptAsync(IdPrompt, opts);
            }
            else
            {
                return await stepContext.NextAsync();
            }
        }

        private async Task<DialogTurnResult> PromptForDescriptionStepAsync(
                                                WaterfallStepContext stepContext,
                                                CancellationToken cancellationToken)
        {
            string problem = (stepContext.Result as FoundChoice)?.Value ?? stepContext.Result as string;

            var hazardState = await HazardProfilerAccessor.GetAsync(stepContext.Context);

            hazardState.Problem = problem;

            await HazardProfilerAccessor.SetAsync(stepContext.Context, hazardState);

            if (string.IsNullOrWhiteSpace(hazardState.Descrcription))
            {
                // prompt for name, if missing
                var opts = new PromptOptions
                {
                    Prompt = new Activity
                    {
                        Type = ActivityTypes.Message,
                        Text = "הזן בבקשה תיאור מפגע",
                    },
                };
                return await stepContext.PromptAsync(DescriptionPrompt, opts);
            }
            else
            {
                return await stepContext.NextAsync();
            }
        }

        private async Task<DialogTurnResult> PromptForProblemStepAsync(
                                                        WaterfallStepContext stepContext,
                                                        CancellationToken cancellationToken)
        {
            // Save name, if prompted.
            var hazardState = await HazardProfilerAccessor.GetAsync(stepContext.Context);
            var lowerCaseName = stepContext.Result as string;
            if (string.IsNullOrWhiteSpace(hazardState.Id) && lowerCaseName != null)
            {
                // Capitalize and set name.
                hazardState.Id = char.ToUpper(lowerCaseName[0]) + lowerCaseName.Substring(1);
                await HazardProfilerAccessor.SetAsync(stepContext.Context, hazardState);
            }

            if (string.IsNullOrWhiteSpace(hazardState.Problem))
            {

                var opts = new PromptOptions
                {
                    Choices = new List<Choice>() { new Choice("מכל אשפה ירוק מלא"),
                    new Choice("פנס רחוב לא תקין"),
                    new Choice("ברז כיבוי מטפטף"),
                    new Choice("גינה ציבורית מלוכלכת"),
                    new Choice("מכל אשפה ירוק לא במקום"),
                    new Choice("צואת כלבים ברחוב"),
                    new Choice("עמוד חסימה לא תקין"),
                    new Choice("יתושים"),
                    },                    
                    Prompt = new Activity
                    {
                        Type = ActivityTypes.Message,
    //                    SuggestedActions = new SuggestedActions(null, new List<CardAction>()
    //{
    //    new CardAction(){ Title = "Blue", Type=ActionTypes.MessageBack, Value="Blue" },
    //    new CardAction(){ Title = "Red", Type=ActionTypes.MessageBack, Value="Red" },
    //    new CardAction(){ Title = "Green", Type=ActionTypes.MessageBack, Value="Green" }
    //}),
                        //Text = $"Hello {hazardState.Id}, what city do you live in?",
                        
                        
                    },
                };
                return await stepContext.PromptAsync(CityPrompt, opts);
            }
            else
            {
                return await stepContext.NextAsync();
            }
        }

        private async Task<DialogTurnResult> DisplayGreetingStateStepAsync(
                                                    WaterfallStepContext stepContext,
                                                    CancellationToken cancellationToken)
        {
            string description = stepContext.Result as string;

            var hazardState = await HazardProfilerAccessor.GetAsync(stepContext.Context);

            hazardState.Descrcription = description;

            await HazardProfilerAccessor.SetAsync(stepContext.Context, hazardState);

            var lowerCaseCity = stepContext.Result as string;
            
            return await GreetUser(stepContext);
        }

        /// <summary>
        /// Validator function to verify if the user name meets required constraints.
        /// </summary>
        /// <param name="promptContext">Context for this prompt.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        private async Task<bool> ValidateId(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // Validate that the user entered a minimum length for their name.
            var value = promptContext.Recognized.Value?.Trim() ?? string.Empty;

            if (IDValidator(value))
            {
                promptContext.Recognized.Value = value;
                return true;
            }
            else
            {
                await promptContext.Context.SendActivityAsync("תעודת זהות לא תקינה");
                return false;
            }
        }

        private async Task<bool> ValidateDescription(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // Validate that the user entered a minimum length for their name.
            var value = promptContext.Recognized.Value?.Trim() ?? string.Empty;

            if (!string.IsNullOrEmpty(value))
            {
                promptContext.Recognized.Value = value;
                return true;
            }
            else
            {
                await promptContext.Context.SendActivityAsync("הזן בבקשה תיאור מפגע");
                return false;
            }
        }

        private bool IDValidator(string id)
        {

            if (id.Length != 9 || String.IsNullOrEmpty(id))
            {
                return false;
            }
            int counter = 0, incNum;
            for (int i = 0; i < id.Length; i++)
            {
                int elmnt;
                int.TryParse(id[i].ToString(), out elmnt);

                incNum = elmnt * ((i % 2) + 1);//multiply digit by 1 or 2
                counter += (incNum > 9) ? incNum - 9 : incNum;//sum the digits up and add to counter
            }
            return (counter % 10 == 0);
        }

        /// <summary>
        /// Validator function to verify if city meets required constraints.
        /// </summary>
        /// <param name="promptContext">Context for this prompt.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        private async Task<bool> ValidateCity(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // Validate that the user entered a minimum lenght for their name
            var value = promptContext.Recognized.Value?.Trim() ?? string.Empty;
            if (value.Length >= CityLengthMinValue)
            {
                promptContext.Recognized.Value = value;
                return true;
            }
            else
            {
                await promptContext.Context.SendActivityAsync($"City names needs to be at least `{CityLengthMinValue}` characters long.");
                return false;
            }
        }

        private async Task<DialogTurnResult> GreetUser(WaterfallStepContext stepContext)
        {
            var context = stepContext.Context;
            var greetingState = await HazardProfilerAccessor.GetAsync(context);

            // Display their profile information and end dialog.
            await context.SendActivityAsync($"מספר פניה 2018-23476");

            await context.SendActivityAsync($"תודה שפנית 106+ תל אביב יפו");

            return await stepContext.EndDialogAsync();
        }

    }
}
