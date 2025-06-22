//
//  LeaderboardViewController.m
//  Turtle
//
//  Created by Giorgi Abelashvili on 5/27/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "LeaderboardViewController.h"
#import "AppSpecificValues.h"
#import "cocos2d.h"
#import "AppSettings.h"

@implementation LeaderboardViewController
@synthesize currentScore;
@synthesize currentLeadboarder;
@synthesize gkLeadboardController;
//
/*+(LeaderboardViewController*)sharedLeaderBoardController
{
    if(!_sharedLeaderBoardController)
        _sharedLeaderBoardController = [[LeaderboardViewController alloc] init];
    
    return _sharedLeaderBoardController;
}*/
//
//
- (id)initWithNibName:(NSString *)nibNameOrNil bundle:(NSBundle *)nibBundleOrNil
{
    //
    self = [super initWithNibName:nibNameOrNil bundle:nibBundleOrNil];
    if (self) {
        // Custom initialization
    }
    //
    return self;
}
//
- (void)dealloc
{
    [super dealloc];
}

- (void)didReceiveMemoryWarning
{
    // Releases the view if it doesn't have a superview.
    [super didReceiveMemoryWarning];
    
    // Release any cached data, images, etc that aren't in use.
}

#pragma mark - View lifecycle

- (void)viewDidLoad
{
    [super viewDidLoad];
    // Do any additional setup after loading the view from its nib.
    //
    self.currentLeadboarder = kLeadBoardID;
    //
    currentScore = 0;
    //
    if([GameCenterManager isGameCenterAvailable])
	{
		gameCenterManager= [[[GameCenterManager alloc] init] autorelease];
		[gameCenterManager setDelegate: self];
        //[gameCenterManager authenticateLocalUser];
		//
        GKLocalPlayer *localPlayer = [GKLocalPlayer localPlayer];
        [localPlayer authenticateWithCompletionHandler:^(NSError *error) 
        {
            if(error == nil)
            {
                NSLog(@"local player authenticated succefully");
            }
            //
            else
            {
                NSLog(@"cannot authenticate local player");
            }
            //
        }];
        //
	}
}
//
//

//
-(void) submitHighScore:(int64_t)score
{
    GKScore *scoreReporter = [[[GKScore alloc] initWithCategory:kLeadBoardID] autorelease];
    scoreReporter.value = score;
    [scoreReporter reportScoreWithCompletionHandler:^(NSError *error) {
        if (error != nil) {
            NSLog(@"Subbmitting Score Failed");
        } else {
            NSLog(@"Subbmitting Score Succeeded");
        }
    }];
    // submit high score
    //[gameCenterManager reportScore: score forCategory: self.currentLeadboarder];

}
//
- (void) showLeaderboard;
{
    GKLeaderboardViewController *gk = [[GKLeaderboardViewController alloc] init];
	if (gk != NULL)
	{
		gk.category = self.currentLeadboarder;
		gk.timeScope = GKLeaderboardTimeScopeAllTime;
		gk.leaderboardDelegate = self;
        [([AppSettings sharedAppSettings]).rootViewController presentModalViewController:gk animated:YES];
        //[self presentModalViewController:gk animated:YES];
    }
    //
}
//
- (void)leaderboardViewControllerDidFinish:(GKLeaderboardViewController *)viewController
{
	[([AppSettings sharedAppSettings]).rootViewController dismissModalViewControllerAnimated: YES];
	[viewController release];
}

- (void)achievementViewControllerDidFinish:(GKAchievementViewController *)viewController;
{
	[([AppSettings sharedAppSettings]).rootViewController dismissModalViewControllerAnimated: YES];
	[viewController release];
}
//
/*- (void) processGameCenterAuth: (NSError*) error
{
	if(error == NULL)
	{
		[gameCenterManager reloadHighScoresForCategory: self.currentLeadboarder];
	}
	else
	{
		UIAlertView* alert= [[[UIAlertView alloc] initWithTitle: @"Game Center Account Required" 
                                                        message: [NSString stringWithFormat: @"Reason: %@", [error localizedDescription]]
                                                       delegate: self cancelButtonTitle: @"Try Again..." otherButtonTitles: @"Cancel"] autorelease];
		[alert show];
	}
	
}*/
- (void)viewDidUnload
{
    [super viewDidUnload];
    // Release any retained subviews of the main view.
    // e.g. self.myOutlet = nil;
}

- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation
{
    // Return YES for supported orientations
    return (interfaceOrientation == UIInterfaceOrientationLandscapeLeft);
}

@end
