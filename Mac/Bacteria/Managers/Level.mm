//
//  Level.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/17/13.
//
//

#import "Level.h"
#import "GameSettingsManager.h"
//
#define ROOT_KEY                    @"Root"
#define LEVEL_NUMBER_KEY            @"number"
#define LEVEL_TIME_KEY              @"time"
#define ENEMIES_CREATE_TIME_KEY     @"enemiescreatetime"
#define ENEMIES_ARRAY_KEY           @"enemies"
#define MAXIMUM_ENEMIES             @"maximumenemies"
#define ENEMIES_GROW_INTERVAL       @"enemiesgrowinterval"
//
#define LEVEL_UNLOCKED_KEY          @"levelisunlocked"
//
@implementation Level
//
@synthesize number;
@synthesize score;
@synthesize unlocked;
@synthesize time;
@synthesize enemiescreatetime;
@synthesize levelEnemies;
@synthesize maximumenemies;
@synthesize enemiesCountGrowConst;
//
-(id) initWithData:(NSDictionary*)levelData
{
    if((self = [super init]))
    {
        //
        number = [[levelData objectForKey:LEVEL_NUMBER_KEY] intValue];
        time = [[levelData objectForKey:LEVEL_TIME_KEY] floatValue];
        enemiescreatetime = [[levelData objectForKey:ENEMIES_CREATE_TIME_KEY] floatValue];
        levelEnemies = [(NSMutableArray*)[levelData objectForKey:ENEMIES_ARRAY_KEY] retain];
        maximumenemies = [[levelData objectForKey:MAXIMUM_ENEMIES] intValue];
        enemiesCountGrowConst = [[levelData objectForKey:ENEMIES_GROW_INTERVAL] intValue];
        //
        switch ([[GameSettingsManager sharedGameSettingsManager] gameDifficulty]) {
            case kNormal:
                // do not nothing
                break;
            case kHard:
                enemiescreatetime-=0.5f;
                break;
            case kImpossible:
                enemiescreatetime-=1.0f;
                break;
            default:
                break;
        }
        //
        return self;
    }
    else return nil;
}
//
/*-(void) setUnlocked:(BOOL)_unlocked
{
    unlocked = _unlocked;
    NSString *baseString = LEVEL_UNLOCKED_KEY;
    baseString = [baseString stringByAppendingString:[NSString stringWithFormat:@"%d",number]];
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    [defaults setObject:[NSNumber numberWithInt:1] forKey:baseString];
    [defaults synchronize];
}
//
-(BOOL) unlocked
{
    NSString *baseString = LEVEL_UNLOCKED_KEY;
    baseString = [baseString stringByAppendingString:[NSString stringWithFormat:@"%d",number]];
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    return ([[defaults objectForKey:baseString] intValue] == 1) ? YES : NO;
}*/
//
-(void) unloadLevel
{
    [levelEnemies removeAllObjects];
    [levelEnemies release];
}
//
@end
