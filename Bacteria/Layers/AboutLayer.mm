//
//  AboutLayer.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/9/13.
//
//

#import "AboutLayer.h"
#import "cocos2d.h"
#import "MainMenuLayer.h"

@implementation AboutLayer
//
+(CCScene*)scene
{
	CCScene *scene = [CCScene node];
	AboutLayer *layer = [AboutLayer node];
	[scene addChild: layer];
	return scene;
}
//
-(id) init
{
    if((self = [super init]))
    {
        self.isKeyboardEnabled = YES;
        self.isMouseEnabled = YES;
        return self;
    }
    return nil;
}
//
-(BOOL) ccKeyDown:(NSEvent *)event
{
    NSString *key = [event characters];
    unichar keyCode = [key characterAtIndex: 0];
    if(keyCode == ESC_KEY)
    {
        [[CCDirector sharedDirector] replaceScene:[MainMenuLayer scene]];
    }
    return YES;
}
//
-(void) onExit
{
    self.isKeyboardEnabled = NO;
    self.isMouseEnabled = NO;
}
//
//
@end
