//
//  AiInput.mm
//  Game
//
//  Created by Giorgi Abelashvili on 10/23/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "AiInput.h"


@implementation AiInput
//
@synthesize mainCharacterPosition;
@synthesize mainCharacterSpeed;
@synthesize mainCharacterRotation;
@synthesize characterIsshooting;
@synthesize cursorPosition;
//
static AiInput *_sharedAiInput = nil;
//
+(AiInput*)sharedAiInput
{
	@synchronized([AiInput class])
	{
		if(!_sharedAiInput)
			_sharedAiInput = [[AiInput alloc] init];
	}
	return _sharedAiInput;
}
//
-(id) init
{
	if((self = [super init]))
	{
		return self;
	}
	//
	return nil;
	//
}
//
-(void) update
{
	
}
//
@end
