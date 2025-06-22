//
//  Inferno.h
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/12/13.
//
//

#import "EnemyAgent.h"

@interface Inferno : EnemyAgent
{
    vector <EnemyAgent*> tentacles;
    int tentaclesCount;
}
@end
