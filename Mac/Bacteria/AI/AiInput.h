//
//  AiInput.h
//  Game
//
//  Created by Giorgi Abelashvili on 10/23/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>


@interface AiInput : NSObject {

	//
	CGPoint mainCharacterPosition;
	float mainCharacterSpeed;
    float mainCharacterRotation;
	NSMutableArray *agentsPosition;
	NSMutableArray *obstacles;
    BOOL characterIsshooting;
    CGPoint cursorPosition;
	//
}
//
@property (nonatomic,assign) CGPoint mainCharacterPosition;
@property (nonatomic,assign) float mainCharacterSpeed;
@property (nonatomic,assign) float mainCharacterRotation;
@property (nonatomic,assign) BOOL characterIsshooting;
@property (nonatomic,assign) CGPoint cursorPosition;
//
+(AiInput*)sharedAiInput;
-(void) update;
//
//
@end
