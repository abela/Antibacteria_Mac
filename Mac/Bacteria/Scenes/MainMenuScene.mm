//
//  MainMenuScene.mm
//  Tanks
//
//  Created by Giorgi Abelashvili on 11/17/10.
//  Copyright 2010 __MyCompanyName__. All rights reserved.
//

#import "MainMenuScene.h"
#import "MainMenuLayer.h"

@implementation MainMenuScene

- (id)init
{
	if(self==[super init])
	{
		[self initLayers];
		
		return self;
	}
	
	return nil;
}

-(void)initLayers
{
	//init Main Menu Layer
	MainMenuLayer *mainLayer = [[MainMenuLayer alloc] init];
	[self addChild:mainLayer z:zMainMenuLayer];
}

@end
