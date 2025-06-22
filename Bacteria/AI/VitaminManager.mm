//
//  VitaminManager.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 6/20/13.
//
//

#import "VitaminManager.h"
#import "Vitamin.h"
#import "Utils.h"

#define VITAMIN_CREATE_RADIUS                   80
//
@interface VitaminManager (PrivateMethods)
-(void) updateVitamins:(int)vitaminScore;
-(void) synchronizeVitaminsCount;
@end
//
VitaminManager *_sharedVitaminManager;
//
@implementation VitaminManager
//
@synthesize vitaminsCount;
//
+(VitaminManager*) sharedVitaminManager
{
    @synchronized([VitaminManager class])
    {
        if(!_sharedVitaminManager)
            _sharedVitaminManager = [[VitaminManager alloc] init];
        return _sharedVitaminManager;
    }
}
//
-(id) init
{
    if((self = [super init]))
    {
        vitaminsCount = [[GameSettingsManager sharedGameSettingsManager] vitaminsCount];
        return self;
    }
    return nil;
}
//
-(CGPoint) getValidBonusSpawnPointAtPoint:(CGPoint)point
{
    
    CGPoint randomPoint = CGPointZero;
    CGPoint newRandPoint = CGPointZero;
    //
    float alpha = arc4random_uniform(360);
    newRandPoint = ccp([Utils getRandomNumber:point.x - VITAMIN_CREATE_RADIUS
                                           to:point.x + VITAMIN_CREATE_RADIUS],
                       [Utils getRandomNumber:point.y - VITAMIN_CREATE_RADIUS
                                           to:point.y + VITAMIN_CREATE_RADIUS]);
    //
    newRandPoint = ccpAdd(randomPoint, newRandPoint);
    float newX = newRandPoint.x;
    float newY = newRandPoint.y;
    //
    if(newX > [[CoreSettings sharedCoreSettings] upperRight].x)
        newX = [[CoreSettings sharedCoreSettings] upperRight].x - BOUNDER_ESCAPE_DELTA;
    if(newX < [[CoreSettings sharedCoreSettings] upperLeft].x)
        newX = [[CoreSettings sharedCoreSettings] upperLeft].x + BOUNDER_ESCAPE_DELTA;
    if(newY > [[CoreSettings sharedCoreSettings] upperRight].y)
        newY = [[CoreSettings sharedCoreSettings] upperRight].y - BOUNDER_ESCAPE_DELTA;
    if(newY < [[CoreSettings sharedCoreSettings] bottomRight].y)
        newY = [[CoreSettings sharedCoreSettings] bottomRight].y + BOUNDER_ESCAPE_DELTA;
    //
    return ccp(newX,newY);

}
//
-(void) createVitaminAtPosition:(CGPoint)position withScore:(int)score
{
    Vitamin *vitamin = [[Vitamin alloc] createAtPosition:position withScore:score];
    vitamins.push_back(vitamin);
    [self updateVitamins:score];
}
//
-(void) updateVitamins:(int)vitaminScore
{
    vitaminsCount+=vitaminScore;
}
//
-(void) synchronizeVitaminsCount
{
    [[GameSettingsManager sharedGameSettingsManager] setVitaminsCount:vitaminsCount];
    [[GameSettingsManager sharedGameSettingsManager] saveGameSettings];
}
//
-(void) update
{
	for (int i = 0; i<(int)vitamins.size(); i++) {
        
        Vitamin *vitamin = vitamins[i];
		if(vitamin.flag != kDealloc)
		{
			[vitamin update];
		}
        else
        {
            [vitamin releaseAgent];
            [vitamin release];
            vitamins.erase(vitamins.begin() + i);
            break;
        }
    }
    //
}

//
-(void) releaseVitaminManager
{
    for (int i = 0; i<(int)vitamins.size(); i++)
    {
        if(vitamins[i].flag!=kDealloc)
        {
            [vitamins[i] releaseAgent];
            [vitamins[i] release];
        }
    }
    vitamins.clear();
    [self synchronizeVitaminsCount];
}
//
@end
