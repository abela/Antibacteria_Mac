//
//  Level.h
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/17/13.
//
//

#import <Foundation/Foundation.h>

@interface Level : NSObject
{
    int number;
    BOOL unlocked;
    int score;
    float time;
    float enemiescreatetime;
    int maximumenemies;
    float enemiesCountGrowConst;
    NSMutableArray *levelEnemies;
}
//
@property (readonly) int number;
@property (nonatomic,assign) int score;
@property (nonatomic,assign) BOOL unlocked;
@property (readonly) float time;
@property (readonly) float enemiescreatetime;
@property (readonly) int maximumenemies;
@property (readonly) float enemiesCountGrowConst;
@property (readonly) NSMutableArray *levelEnemies;
//
-(id) initWithData:(NSDictionary*)levelData;
-(void) unloadLevel;
@end
